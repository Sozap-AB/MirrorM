using MirrorM.AdapterInterface;
using MirrorM.AdapterInterface.Query;
using MirrorM.AdapterInterface.Query.Conditions;
using MirrorM.AdapterInterface.Query.Connections;
using MirrorM.Attributes;
using MirrorM.Common;
using MirrorM.Internal.Query;
using MirrorM.Internal.Query.Builder;
using MirrorM.Internal.Query.Cache;
using MirrorM.Internal.Query.Modifications;
using MirrorM.Internal.Query.Modifications.Connections;
using MirrorM.Internal.Query.Storage;
using MirrorM.TypeConversion;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MirrorM.Internal
{
    internal class Context : IContext, IContextInternal
    {
        private enum GetQueryStrategy
        {
            Default, // load to storage and (filter + sort) against the storage
            UseDbSelection, // load to storage, filter loaded and return
        }

        private sealed class Memory
        {
            public HashSet<QueryCacheItem> ExecutedQueriesCache { get; } = new HashSet<QueryCacheItem>();
            public IDictionary<Guid, EntityBase> EntityStorage { get; } = new Dictionary<Guid, EntityBase>();
            public IDictionary<string, ConnectionTableStorage> ConnectionStorage { get; } = new Dictionary<string, ConnectionTableStorage>();
        }

        private IDatabaseConnection? DatabaseConnectionOptional { get; set; }
        private Func<Task<IDatabaseConnection>>? DatabaseConnectionInitializer { get; }

        private IDatabaseConnection DatabaseConnection => DatabaseConnectionOptional ?? throw new InvalidOperationException("Database connection is not initialized!");
        private IEnumerable<ITypeConverter> TypeConverters { get; }

        private Memory ContextMemory { get; set; }

        private HashSet<QueryCacheItem> ExecutedQueriesCache => ContextMemory.ExecutedQueriesCache;
        private IDictionary<Guid, EntityBase> EntityStorage => ContextMemory.EntityStorage;
        private IDictionary<string, ConnectionTableStorage> ConnectionStorage => ContextMemory.ConnectionStorage;

        internal ISuperContext? SuperContext { get; private set; }

        public Context(IDatabaseConnection databaseConnection, IEnumerable<ITypeConverter> typeConverters) : this(typeConverters)
        {
            DatabaseConnectionOptional = databaseConnection;
            DatabaseConnectionInitializer = null;
        }

        public Context(Func<Task<IDatabaseConnection>> databaseConnectionInitializer, IEnumerable<ITypeConverter> typeConverters) : this(typeConverters)
        {
            DatabaseConnectionOptional = null;
            DatabaseConnectionInitializer = databaseConnectionInitializer;
        }

        private Context(IEnumerable<ITypeConverter> typeConverters)
        {
            TypeConverters = typeConverters;

            ContextMemory = new Memory();
        }

        public async Task InitializeAsync()
        {
            // if DatabaseConnectionInitializer == null, we assume context
            // has been created with database connection constructor
            if (DatabaseConnectionInitializer == null)
                throw new InvalidOperationException("Context created this way shouldn't be initialized manually!");

            if (DatabaseConnectionOptional == null)
                DatabaseConnectionOptional = await DatabaseConnectionInitializer();
        }

        public void SetSuperContext(ISuperContext? superContext)
        {
            SuperContext = superContext;
        }

        public ISuperContext? GetSuperContext()
        {
            return SuperContext;
        }

        public void SetSqlInterceptor(Action<string, IEnumerable<DbParameter>>? sqlInterceptor)
        {
            DatabaseConnection.SetSqlInterceptor(sqlInterceptor);
        }

        public IQuery<T> Query<T>() where T : EntityBase
        {
            return new QueryBuilder<T>(this);
        }

        public Task<R> ExecuteSumQueryAsync<T, R>(QueryBuilder<T> builder) where T : EntityBase
        {
            //TODO: we should take uncommitted changes into account

            return DatabaseConnection.ExecuteSumAsync<R>(builder);
        }

        public Task<R?> ExecuteMaxQueryAsync<T, R>(QueryBuilder<T> builder) where T : EntityBase where R : struct
        {
            //TODO: we should take uncommitted changes into account

            return DatabaseConnection.ExecuteMaxAsync<R>(builder);
        }

        public Task<long> ExecuteCountQueryAsync<T>(QueryBuilder<T> builder) where T : EntityBase
        {
            //TODO: we should take uncommitted changes into account

            return DatabaseConnection.ExecuteCountAsync(builder);
        }

        public Task<bool> ExecuteExistsQueryAsync<T>(QueryBuilder<T> builder) where T : EntityBase
        {
            //TODO: we should take uncommitted changes into account

            return DatabaseConnection.ExecuteExistsAsync(builder);
        }

        public IAsyncEnumerable<T> ExecuteSqlQueryAsync<T>(string sql, params SqlParameter[] parameters) where T : EntityBase
        {
            return DatabaseConnection.ExecuteRawSelectAsync(
                sql,
                ConvertParametersForWriting(parameters),
                rec => GetOrSaveEntityToStorage<T>(rec)
            );
        }

        public async IAsyncEnumerable<T> ExecuteGetQueryAsync<T>(QueryBuilder<T> builder) where T : EntityBase
        {
            // Execution plan:

            // 1. Try to handle query with fast track, if possible (currently only covers getById case)
            // 2. Check against cache, if cache is covering the query, execute conditions & sorting against storage
            // 3. Build SQL query and execute, put result to storage
            // 4.1 By default, we're ignoreing returned results after putting it to storage and filtering & sorting storage to get result
            // 4.2 If query contains Skip or Take with sortings, we're taking returned results and filtering every item of it
            // 5. Execute conditions & sorting against storage

            // trying the easy way to handle query

            if (TryGetFastTrackResult<T>(builder, out var ftr))
            {
                foreach (var item in ftr)
                    yield return item;

                yield break;
            }

            // calculating strategy and working with cache

            var strategy = CalculateGetQueryStrategy(builder);
            QueryCacheItem? currentCacheItem = null;

            if (IsQueryCachable(builder))
            {
                currentCacheItem = new QueryCacheItem(builder);

                if (ExecutedQueriesCache.Any(ci => ci.IsCoveringCacheItem(currentCacheItem)))
                {
                    foreach (var item in GetFilteredAndSortedEntities<T>(builder))
                        yield return item;

                    yield break;
                }
            }

            // executing SQL and saving to storage

            var connectionConditions = builder.Conditions.OfType<ExpressionConnectionMatch>().ToArray(); //TODO: check if ToArray is not needed

            await foreach (var entity in ExecuteSqlAndHandleRecordsAsync( //TODO: filter duplicates
                builder,
                rec => GetOrSaveEntitiesToStorage(rec, builder)
            ))
            {
                if (entity is T) // it's important, cause item might be loaded relation
                {
                    SaveConnectionsToStorage(entity, connectionConditions);

                    if (strategy == GetQueryStrategy.UseDbSelection)
                    {
                        if (entity.IsUpdated && !FilterEntity(entity, builder.Conditions))
                            continue;

                        yield return (T)entity;
                    }
                }
            }

            if (strategy == GetQueryStrategy.UseDbSelection)
                yield break;

            if (currentCacheItem != null)
                ExecutedQueriesCache.Add(currentCacheItem);

            // filtering and sorting against storage

            foreach (var item in GetFilteredAndSortedEntities<T>(builder))
                yield return item;
        }

        private bool TryGetFastTrackResult<T>(IEntityQuerySchema schema, out IEnumerable<T> result) where T : EntityBase
        {
            // currently fast track only works for single entity queried by id

            result = Array.Empty<T>();

            if (schema.TakeCount.HasValue && schema.TakeCount.Value != 1)
                return false;

            if (schema.SkipCount > 0)
                return false;

            if (schema.Conditions.Count() != 1)
                return false;

            if (!(schema.Conditions.First() is ExpressionBinary eb))
                return false;

            if (eb.Op != ExpressionBinary.Operation.Equal)
                return false;

            if (eb.Left is ExpressionField lef)
            {
                if (lef.FieldName == EntityBase.FIELD_ID
                && eb.Right is ExpressionConst rec
                && rec.Value is Guid rid
                && EntityStorage.TryGetValue(rid, out var lres))
                {
                    result = new[] { (T)lres };

                    return true;
                }
            }
            else if (eb.Right is ExpressionField rexpf
                && rexpf.FieldName == EntityBase.FIELD_ID
                && eb.Left is ExpressionConst lec
                && lec.Value is Guid lid
                && EntityStorage.TryGetValue(lid, out var rres))
            {
                result = new[] { (T)rres };

                return true;
            }

            return false;
        }

        private static GetQueryStrategy CalculateGetQueryStrategy(IEntityQuerySchema schema)
        {
            if (schema.Conditions.Any(x => x is ExpressionRawSql))
                return GetQueryStrategy.UseDbSelection;

            if (schema.SkipCount > 0)
                return GetQueryStrategy.UseDbSelection;

            return GetQueryStrategy.Default;
        }

        private static bool IsQueryCachable(IEntityQuerySchema schema)
        {
            if (schema.SkipCount > 0)
                return false;

            return true;
        }

        private IEnumerable<T> GetFilteredAndSortedEntities<T>(IEntityQuerySchema schema) where T : EntityBase
        {
            IEnumerable<T> result = EntityStorage
                .Select(x => x.Value)
                .OfType<T>()
                .Where(e => FilterEntity(e, schema.Conditions));

            IOrderedEnumerable<T>? orderedResult = null;

            foreach (var sorting in schema.Sortings)
            {
                if (orderedResult == null)
                {
                    orderedResult = sorting.Ascending
                        ? result.OrderBy(e => e.Fields.GetValue<object>(sorting.PropertyName))
                        : result.OrderByDescending(e => e.Fields.GetValue<object>(sorting.PropertyName));
                }
                else
                {
                    orderedResult = sorting.Ascending
                        ? orderedResult.ThenBy(e => e.Fields.GetValue<object>(sorting.PropertyName))
                        : orderedResult.ThenByDescending(e => e.Fields.GetValue<object>(sorting.PropertyName));
                }
            }

            result = orderedResult ?? result;

            if (schema.TakeCount.HasValue)
                result = result.Take(schema.TakeCount.Value);

            return result;
        }

        private bool FilterEntity(EntityBase entity, IEnumerable<ExpressionBase> conditions)
        {
            return conditions.All(c => ExpressionEvaluator.EvaluateCondition(entity, c, this));
        }

        private IAsyncEnumerable<EntityBase> ExecuteSqlAndHandleRecordsAsync(IEntityQuerySchema schema, Func<IDataReader, IEnumerable<EntityBase>> handler)
        {
            return DatabaseConnection.ExecuteSelectAsync(schema, handler);
        }

        private T CreateEntity<T>(Type t, IFields fields) where T : EntityBase
        {
            return (T)Activator.CreateInstance(t, this, fields)!;
        }

        private void SaveConnectionsToStorage(EntityBase entity, IEnumerable<ExpressionConnectionMatch> conditions)
        {
            foreach (var condition in conditions)
            {
                GetOrCreateConnectionTableStorage(condition.ConnectionTable, condition.OwnerKey, condition.ForeignKey)
                    .AddConnection((condition.OwnerKey, entity.Id), (condition.ForeignKey, condition.Value), EntityState.Loaded);
            }
        }

        private IEnumerable<EntityBase> GetOrSaveEntitiesToStorage(IDataReader record, IEntityQuerySchema query)
        {
            IEnumerable<Type> CollectEntityTypes()
            {
                yield return query.EntityType;

                foreach (var j in query.Joins)
                {
                    yield return j.ForeignType;
                }
            }

            var entityTypes = CollectEntityTypes();

            int typeIndex = 0;
            int i = 0;

            while (i < record.FieldCount)
            {
                var id = record.GetGuid(i);

                Dictionary<string, object?>? collection = !record.IsDBNull(i) ? new Dictionary<string, object?>() {
                    { record.GetName(i), id }
                } : null;

                i++;

                while (i < record.FieldCount && record.GetName(i) != EntityBase.FIELD_ID)
                {
                    collection?.Add(record.GetName(i), ReadValue(record, i));

                    i++;
                }

                if (collection != null)
                {
                    if (EntityStorage.TryGetValue(id, out var existingEntity))
                    {
                        //TODO: update/merge fields if object exists

                        yield return existingEntity;
                    }
                    else
                    {
                        yield return CreateEntity<EntityBase>(
                            DetectType(entityTypes.ElementAt(typeIndex), collection),
                            new FieldCollection(collection)
                        );
                    }
                }

                typeIndex++;
            }
        }

        private T GetOrSaveEntityToStorage<T>(IDataReader record) where T : EntityBase
        {
            var id = record.GetGuid(0);

            Dictionary<string, object?> collection = new Dictionary<string, object?>() {
                { record.GetName(0), id }
            };

            for (int i = 1; i < record.FieldCount; i++)
            {
                collection.Add(record.GetName(i), ReadValue(record, i));
            }

            if (EntityStorage.TryGetValue(id, out var existingEntity))
            {
                //TODO: update/merge fields if object exists

                return (T)existingEntity;
            }
            else
            {
                return CreateEntity<T>(
                    DetectType(typeof(T), collection),
                    new FieldCollection(collection)
                );
            }
        }

        private static Type DetectType(Type defaultType, IReadOnlyDictionary<string, object?> fields)
        {
            var subTypes = defaultType.GetCustomAttribute<EntityAttribute>()!.SubTypes;

            if (subTypes != null)
            {
                if (fields.TryGetValue(EntityBase.FIELD_TYPE, out var typeName))
                {
                    return subTypes!.First(x => x.Name == (string)typeName!);
                }
                else
                {
                    return subTypes!.First();
                }
            }

            return defaultType;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            DatabaseConnectionOptional?.Dispose();
        }

        public IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ExecuteSqlQueryAndGetRawResultAsync(string sql, params SqlParameter[] parameters)
        {
            return DatabaseConnection.ExecuteRawSelectAsync(sql, ConvertParametersForWriting(parameters), rec =>
            {
                var dict = new Dictionary<string, object?>(capacity: rec.FieldCount);

                for (int i = 0; i < rec.FieldCount; i++)
                {
                    dict[rec.GetName(i)] = ReadValue(rec, i);
                }

                return dict;
            });
        }

        private object? ReadValue(IDataReader reader, int index)
        {
            foreach (var typeConverter in TypeConverters)
            {
                if (typeConverter.TryConvertOnRead(reader, index, out var result))
                    return result;
            }

            return reader.GetValue(index);
        }

        private SqlParameterValue? TryConvertForWriting(object? value)
        {
            if (value == null)
                return new SqlParameterValue(DBNull.Value, null);

            foreach (var typeConverter in TypeConverters)
            {
                if (typeConverter.TryConvertToWrite(value, out var result))
                    return result;
            }

            return null;
        }

        private SqlParameterValue ConvertValueForWriting(object? value)
        {
            return TryConvertForWriting(value) ?? new SqlParameterValue(value!, null);
        }

        private IEnumerable<SqlParameter> ConvertParametersForWriting(IEnumerable<SqlParameter> parameters)
        {
            return parameters.Select(p =>
            {
                var v = TryConvertForWriting(p.Value.Value);

                return v != null ? new SqlParameter(p.Name, v) : p;
            });
        }

        public Task<int> ExecuteSqlCommandAsync(string sql, params SqlParameter[] parameters)
        {
            return DatabaseConnection.ExecuteRawCommandAsync(sql, ConvertParametersForWriting(parameters));
        }

        public async Task ExecuteTransactionAsync(Func<Task> action)
        {
            void CopyState(Memory from, Memory to)
            {
                foreach (var item in from.EntityStorage)
                {
                    if (!to.EntityStorage.ContainsKey(item.Key))
                    {
                        to.EntityStorage[item.Key] = CreateEntity<EntityBase>(
                            item.Value.GetType(),
                            new FieldCollection((FieldCollection)item.Value.Fields)
                        );
                    }
                    else if (item.Value.IsUpdated)
                    {
                        to.EntityStorage[item.Key].UpdateFromCommited(item.Value);
                    }
                }
            }

            var originalContextMemory = ContextMemory;

            var tempMemory = new Memory();

            ContextMemory = tempMemory;

            try
            {
                await DatabaseConnection.ExecuteWrappedInTransactionAsync(async () =>
                {
                    await action();

                    await CommitAsync(false);
                });
            }
            finally
            {
                ContextMemory = originalContextMemory;
            }

            CopyState(tempMemory, ContextMemory);
        }

        public async Task CommitAsync()
        {
            await CommitAsync(true);
        }

        public async Task CommitAsync(bool createTransaction)
        {
            List<IModificationSchema> commands = new List<IModificationSchema>();

            // collecing entities changes

            var items = EntityStorage
                .Where(item =>
                {
                    if (item.Value.EntityState == EntityState.Loaded &&
                        item.Value.UpdatedFields.Any())
                        return true;

                    if (item.Value.EntityState == EntityState.New)
                        return true;

                    if (item.Value.EntityState == EntityState.Deleted)
                        return true;

                    return false;
                })
                .Select(item => item.Value);

            commands.AddRange(
                items.Select<EntityBase, IEntityModificationSchema>(i =>
                {
                    switch (i.EntityState)
                    {
                        case EntityState.New:
                            return new EntityInsertStatement(
                                i.GetType(),
                                i.Fields.GetEnumerable().Select(f => new SqlParameter(f.Key, ConvertValueForWriting(f.Value)))
                            );
                        case EntityState.Loaded:
                            return new EntityUpdateStatement(i.GetType(), new ExpressionBase[] {
                                new ExpressionBinary(ExpressionBinary.Operation.Equal, new ExpressionField(EntityBase.FIELD_ID), new ExpressionConst(i.Id)),
                                new ExpressionBinary(ExpressionBinary.Operation.Equal, new ExpressionField(EntityBase.FIELD_VERSION), new ExpressionConst(i.OriginalVersion))
                            }, i.GetModifications().Select(f => new SqlParameter(f.Key, ConvertValueForWriting(f.Value))));
                        case EntityState.Deleted:
                            return new EntityDeleteStatement(i.GetType(), new ExpressionBase[] {
                                new ExpressionBinary(ExpressionBinary.Operation.Equal, new ExpressionField(EntityBase.FIELD_ID), new ExpressionConst(i.Id)),
                                new ExpressionBinary(ExpressionBinary.Operation.Equal, new ExpressionField(EntityBase.FIELD_VERSION), new ExpressionConst(i.OriginalVersion))
                            });
                        default:
                            throw new InvalidOperationException();
                    }
                })
            );

            // collecting connections changes

            commands.AddRange(
                ConnectionStorage
                    .SelectMany(connectionTable =>
                    {
                        return connectionTable.Value
                            .Connections
                            .Where(x => x.State == EntityState.New || x.State == EntityState.Deleted)
                            .Select<Connection, IConnectionModificationSchema>(connection =>
                            {
                                switch (connection.State)
                                {
                                    case EntityState.New:
                                        return new ConnectionInsertStatement(
                                            connectionTable.Key,
                                            connectionTable.Value.Key1,
                                            connection.Key1Value,
                                            connectionTable.Value.Key2,
                                            connection.Key2Value
                                        );
                                    case EntityState.Deleted:
                                        return new ConnectionDeleteStatement(
                                            connectionTable.Key,
                                            connectionTable.Value.Key1,
                                            connection.Key1Value,
                                            connectionTable.Value.Key2,
                                            connection.Key2Value
                                        );
                                    default:
                                        throw new InvalidOperationException();
                                }
                            });
                    })
            );

            // saving changes

            await DatabaseConnection.ExecuteCommandBatchAsync(commands, createTransaction);
        }

        public void Add(EntityBase entity)
        {
            if (EntityStorage.ContainsKey(entity.Id))
                throw new InvalidOperationException($"Entity with id {entity.Id} already exists in storage");

            EntityStorage[entity.Id] = entity;
        }

        public void AddConnection(string connectionTable, string key1, Guid id1, string key2, Guid id2)
        {
            GetOrCreateConnectionTableStorage(connectionTable, key1, key2).AddConnection((key1, id1), (key2, id2), EntityState.New);
        }

        public void RemoveConnection(string connectionTable, string key1, Guid id1, string key2, Guid id2)
        {
            GetOrCreateConnectionTableStorage(connectionTable, key1, key2).MarkConnectionRemoved((key1, id1), (key2, id2));
        }

        private ConnectionTableStorage GetOrCreateConnectionTableStorage(string connectionTable, string key1, string key2)
        {
            if (!ConnectionStorage.ContainsKey(connectionTable))
                ConnectionStorage[connectionTable] = new ConnectionTableStorage(connectionTable, key1, key2);

            return ConnectionStorage[connectionTable];
        }

        public ConnectionTableStorage? GetConnectionTableStorage(string connectionTable)
        {
            return ConnectionStorage.ContainsKey(connectionTable) ? ConnectionStorage[connectionTable] : null;
        }
    }
}
