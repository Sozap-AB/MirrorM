using FishingTourServer.Sys.Services.Data.Database.Attributes;
using MirrorM.AdapterInterface;
using MirrorM.AdapterInterface.Query;
using MirrorM.AdapterInterface.Query.Conditions;
using MirrorM.AdapterInterface.Query.Connections;
using MirrorM.Common;
using MirrorM.Internal.Query;
using MirrorM.Internal.Query.Builder;
using MirrorM.Internal.Query.Cache;
using MirrorM.Internal.Query.Modifications;
using MirrorM.Internal.Query.Modifications.Connections;
using MirrorM.Internal.Query.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MirrorM.Internal
{
    internal class Context : IContext, IContextInternal
    {
        private enum GetQueryStrategy
        {
            Default, // load to storage and filter + sort against the storage
            UseDbSelection, // load to storage, filter loaded and return
        }

        private sealed class Memory
        {
            public HashSet<QueryCacheItem> ExecutedQueriesCache { get; } = new HashSet<QueryCacheItem>();
            public Dictionary<Guid, Entity> EntityStorage { get; } = new Dictionary<Guid, Entity>();
            public Dictionary<string, ConnectionTableStorage> ConnectionStorage { get; } = new Dictionary<string, ConnectionTableStorage>();
        }

        private IDatabaseConnection DatabaseConnection { get; }

        private Memory ContextMemory { get; set; }

        private HashSet<QueryCacheItem> ExecutedQueriesCache => ContextMemory.ExecutedQueriesCache;
        private Dictionary<Guid, Entity> EntityStorage => ContextMemory.EntityStorage;
        private Dictionary<string, ConnectionTableStorage> ConnectionStorage => ContextMemory.ConnectionStorage;

        public Context(IDatabaseConnection databaseConnection)
        {
            DatabaseConnection = databaseConnection;

            ContextMemory = new Memory();
        }

        public IQuery<T> Query<T>() where T : Entity
        {
            return new QueryBuilder<T>(this);
        }

        public Task<R> ExecuteSumQueryAsync<T, R>(QueryBuilder<T> builder) where T : Entity
        {
            //TODO: we should take uncommitted changes into account

            return DatabaseConnection.ExecuteSumAsync<R>(builder);
        }

        public Task<R?> ExecuteMaxQueryAsync<T, R>(QueryBuilder<T> builder) where T : Entity where R : struct
        {
            //TODO: we should take uncommitted changes into account

            return DatabaseConnection.ExecuteMaxAsync<R>(builder);
        }

        public Task<int> ExecuteCountQueryAsync<T>(QueryBuilder<T> builder) where T : Entity
        {
            //TODO: we should take uncommitted changes into account

            return DatabaseConnection.ExecuteCountAsync(builder);
        }

        public Task<bool> ExecuteExistsQueryAsync<T>(QueryBuilder<T> builder) where T : Entity
        {
            //TODO: we should take uncommitted changes into account

            return DatabaseConnection.ExecuteExistsAsync(builder);
        }

        public IAsyncEnumerable<T> ExecuteSqlQueryAsync<T>(string sql, params SqlParameter[] parameters) where T : Entity
        {
            return DatabaseConnection.ExecuteRawSelectAsync(sql, parameters, rec => GetOrSaveEntityToStorage<T>(rec));
        }

        public async IAsyncEnumerable<T> ExecuteGetQueryAsync<T>(QueryBuilder<T> builder) where T : Entity
        {
            // Execution plan:

            // 1. Check against cache, if cache is covering the query, execute conditions & sorting against storage
            // 2. Build SQL query and execute, put result to storage
            // 2.1 By default, we're ignoreing returned results after putting it to storage and filtering & sorting storage to get result
            // 2.2 If query contains Skip or Take, we're taking returned results and filtering every item of it
            // 3. Execute conditions & sorting against storage

            var strategy = CalculateGetQueryStrategy(builder);

            var currentCacheItem = new QueryCacheItem(builder.Conditions);

            if (ExecutedQueriesCache.Any(ci => ci.IsCoveringCacheItem(currentCacheItem)))
            {
                foreach (var item in GetFilteredAndSortedEntities<T>(builder))
                    yield return item;
            }

            // executing SQL and saving to storage

            var connectionConditions = builder.Conditions.OfType<ExpressionConnectionMatch>().ToArray(); //TODO: check if ToArray is not needed

            await foreach (var entity in ExecuteSqlAndHandleRecordsAsync( //TODO: filter duplicates
                builder,
                rec => GetOrSaveEntitiesToStorage(rec, builder)
            ))
            {
                if (entity is T)
                {
                    SaveConnectionsToStorage(entity, connectionConditions);

                    if (strategy == GetQueryStrategy.UseDbSelection) // it's important, cause item might be loaded relation
                    {
                        if (entity.IsUpdated && !FilterEntity(entity, builder.Conditions))
                            continue;

                        yield return (T)entity;
                    }
                }
            }

            // filtering and sorting against storage

            foreach (var item in GetFilteredAndSortedEntities<T>(builder))
                yield return item;
        }

        private static GetQueryStrategy CalculateGetQueryStrategy(IEntityQuerySchema constraints)
        {
            if (constraints.SkipCount > 0 || constraints.TakeCount.HasValue)
                return GetQueryStrategy.UseDbSelection;

            return GetQueryStrategy.Default;
        }

        private IEnumerable<T> GetFilteredAndSortedEntities<T>(IEntityQuerySchema constraints) where T : Entity
        {
            IEnumerable<T> result = ConnectionStorage
                .OfType<T>()
                .Where(e => FilterEntity(e, constraints.Conditions));

            IOrderedEnumerable<T>? orderedResult = null;

            foreach (var sorting in constraints.Sortings)
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

            return orderedResult ?? result;
        }

        private bool FilterEntity(Entity entity, IEnumerable<ExpressionBase> conditions)
        {
            return conditions.All(c => ExpressionEvaluator.EvaluateCondition(entity, c, this));
        }

        private IAsyncEnumerable<Entity> ExecuteSqlAndHandleRecordsAsync(IEntityQuerySchema schema, Func<IDataReader, IEnumerable<Entity>> handler)
        {
            return DatabaseConnection.ExecuteSelectAsync(schema, handler);
        }

        private T CreateEntity<T>(Type t, IFields fields) where T : Entity
        {
            return (T)Activator.CreateInstance(t, this, fields)!;
        }

        private void SaveConnectionsToStorage(Entity entity, IEnumerable<ExpressionConnectionMatch> conditions)
        {
            foreach (var condition in conditions)
            {
                GetOrCreateConnectionTableStorage(condition.ConnectionTable, condition.OwnerKey, condition.ForeignKey)
                    .AddConnection((condition.OwnerKey, entity.Id), (condition.ForeignKey, condition.Value), EntityState.Loaded);
            }
        }

        private IEnumerable<Entity> GetOrSaveEntitiesToStorage(IDataReader record, IEntityQuerySchema query)
        {
            Type DetectType(Type defaultType, IReadOnlyDictionary<string, object?> fields)
            {
                var subTypes = defaultType.GetCustomAttribute<EntityAttribute>()!.SubTypes;

                if (subTypes != null)
                {
                    if (fields.TryGetValue(Entity.FIELD_TYPE, out var typeName))
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

            T SaveObjectToStorage<T>(T obj) where T : Entity
            {
                if (!EntityStorage.ContainsKey(obj.Id))
                {
                    EntityStorage[obj.Id] = obj;
                }

                //TODO: update/merge fields if object exists

                return (T)EntityStorage[obj.Id];
            }

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
                object id = record.GetValue(i);

                Dictionary<string, object?>? collection = id != DBNull.Value ? new Dictionary<string, object?>() {
                    { record.GetName(i), id }
                } : null;

                i++;

                while (i < record.FieldCount && record.GetName(i) != Entity.FIELD_ID)
                {
                    collection?.Add(record.GetName(i), record.GetValue(i));

                    i++;
                }

                if (collection != null)
                {
                    yield return SaveObjectToStorage(CreateEntity<Entity>(
                        DetectType(entityTypes.ElementAt(typeIndex), collection),
                        new FieldCollection(collection)
                    ));
                }

                typeIndex++;
            }
        }

        private T GetOrSaveEntityToStorage<T>(IDataReader record) where T : Entity
        {
            T SaveObjectToStorage(T obj)
            {
                if (!EntityStorage.ContainsKey(obj.Id))
                {
                    EntityStorage[obj.Id] = obj;
                }

                //TODO: update/merge fields if object exists

                return obj;
            }

            object id = record.GetValue(0);

            Dictionary<string, object?> collection = new Dictionary<string, object?>() {
                { record.GetName(0), id }
            };

            for (int i = 0; i < record.FieldCount; i++)
            {
                collection.Add(record.GetName(i), record.GetValue(i));
            }

            return SaveObjectToStorage(CreateEntity<T>(
                typeof(T),
                new FieldCollection(collection)
            ));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            DatabaseConnection.Dispose();
        }

        public IAsyncEnumerable<IReadOnlyDictionary<string, object>> ExecuteSqlQueryAndGetRawResultAsync(string sql, params SqlParameter[] parameters)
        {
            return DatabaseConnection.ExecuteRawSelectAsync(sql, parameters, rec =>
            {
                var dict = new Dictionary<string, object>(capacity: rec.FieldCount);

                for (int i = 0; i < rec.FieldCount; i++)
                {
                    dict[rec.GetName(i)] = rec.GetValue(i);
                }

                return dict;
            });
        }

        public Task<int> ExecuteSqlCommandAsync(string sql, params SqlParameter[] parameters)
        {
            return DatabaseConnection.ExecuteRawCommandAsync(sql, parameters);
        }

        public async Task ExecuteTransactionAsync(Func<Task> action)
        {
            void CopyState(Memory from, Memory to)
            {
                foreach (var item in from.EntityStorage)
                {
                    if (!to.EntityStorage.ContainsKey(item.Key))
                    {
                        to.EntityStorage[item.Key] = CreateEntity<Entity>(
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
                items.Select<Entity, IEntityModificationSchema>(i =>
                {
                    switch (i.EntityState)
                    {
                        case EntityState.New:
                            return new EntityInsertStatement(
                                i.GetType(),
                                i.Fields.GetEnumerable().ToDictionary(f => f.Key, f => f.Value)
                            );
                        case EntityState.Loaded:
                            return new EntityUpdateStatement(i.GetType(), new ExpressionBase[] {
                                new ExpressionBinary(ExpressionBinary.Operation.Equal, new ExpressionField(Entity.FIELD_ID), new ExpressionConst(i.Id)),
                                new ExpressionBinary(ExpressionBinary.Operation.Equal, new ExpressionField(Entity.FIELD_VERSION), new ExpressionConst(i.OriginalVersion))
                            }, i.GetModifications());
                        case EntityState.Deleted:
                            return new EntityDeleteStatement(i.GetType(), new ExpressionBase[] {
                                new ExpressionBinary(ExpressionBinary.Operation.Equal, new ExpressionField(Entity.FIELD_ID), new ExpressionConst(i.Id)),
                                new ExpressionBinary(ExpressionBinary.Operation.Equal, new ExpressionField(Entity.FIELD_VERSION), new ExpressionConst(i.OriginalVersion))
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

        public void Add(Entity entity)
        {
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
