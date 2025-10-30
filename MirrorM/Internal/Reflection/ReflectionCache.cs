using FishingTourServer.Sys.Services.Data.Database.Attributes;
using MirrorM.Common;
using MirrorM.Internal.Query.Builder;
using MirrorM.Internal.Query.Storage;
using MirrorM.Internal.Relations;
using MirrorM.Relations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MirrorM.Internal.Reflection
{
    internal class ReflectionCache : IReflectionCache
    {
        public class ReflectionCacheItem
        {
            public Type EntityType { get; }

            public Dictionary<PropertyInfo, ReflectionCacheItemPropertyBase> Properties { get; } = new Dictionary<PropertyInfo, ReflectionCacheItemPropertyBase>();

            public ReflectionCacheItem(Type entityType)
            {
                EntityType = entityType;
            }
        }

        public class ReflectionCacheItemPropertyBase
        {
            public PropertyInfo Property { get; }

            public ReflectionCacheItemPropertyBase(PropertyInfo property)
            {
                Property = property;
            }
        }

        public class ReflectionCacheItemPropertyManyToManyRelation : ReflectionCacheItemPropertyBase
        {
            public string OwnConnectionKey { get; }
            public string ForeignConnectionKey { get; }
            public string ConnectionTableName { get; }

            public ReflectionCacheItemPropertyManyToManyRelation(PropertyInfo property, string ownConnectionKey, string foreignConnectionKey, string connectionTableName) : base(property)
            {
                OwnConnectionKey = ownConnectionKey;
                ForeignConnectionKey = foreignConnectionKey;
                ConnectionTableName = connectionTableName;
            }
        }

        public class ReflectionCacheItemPropertyIdToFieldRelation : ReflectionCacheItemPropertyBase
        {
            public string ForeignKey { get; }

            public ReflectionCacheItemPropertyIdToFieldRelation(PropertyInfo property, string foreignKey) : base(property)
            {
                ForeignKey = foreignKey;
            }
        }

        private sealed class FieldsMock : IFields, IFieldsInternal
        {
            public T GetValue<T>(string field)
            {
                if (field == Entity.FIELD_VERSION)
                {
                    return (T)(object)0L;
                }

                throw new NotSupportedException();
            }

            public void SetValue<T>(string field, T value)
            {
                throw new NotSupportedException();
            }

            public IEnumerable<KeyValuePair<string, object?>> GetEnumerable()
            {
                throw new NotSupportedException();
            }

            public object? GetRawValue(string key)
            {
                throw new NotSupportedException();
            }

            public IReadOnlyDictionary<string, object?> GetStorage()
            {
                throw new NotSupportedException();
            }

            public void CopyFields(IFieldsInternal fields)
            {
                throw new NotSupportedException();
            }

            public void CopyFields(IFieldsInternal fields, IEnumerable<string> fieldNames)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class ContextMock : IContext, IContextInternal
        {
            public void Add(Entity entity)
            {
                // ignore
            }

            public void AddConnection(string connectionTable, string key1, Guid id1, string key2, Guid id2)
            {
                throw new NotSupportedException();
            }

            public void RemoveConnection(string connectionTable, string key1, Guid id1, string key2, Guid id2)
            {
                throw new NotSupportedException();
            }

            public Task CommitAsync()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }

            public IAsyncEnumerable<T> ExecuteGetQueryAsync<T>(QueryBuilder<T> builder) where T : Entity
            {
                throw new NotSupportedException();
            }

            public Task<int> ExecuteSqlCommandAsync(string sql, params SqlParameter[] parameters)
            {
                throw new NotSupportedException();
            }

            public IAsyncEnumerable<IReadOnlyDictionary<string, object>> ExecuteSqlQueryAndGetRawResultAsync(string sql, params SqlParameter[] parameters)
            {
                throw new NotSupportedException();
            }

            public IAsyncEnumerable<T> ExecuteSqlQueryAsync<T>(string sql, params SqlParameter[] parameters) where T : Entity
            {
                throw new NotSupportedException();
            }

            public Task ExecuteTransactionAsync(Func<Task> action)
            {
                throw new NotSupportedException();
            }

            public ConnectionTableStorage? GetConnectionTableStorage(string connectionTable)
            {
                throw new NotSupportedException();
            }

            public IQuery<T> Query<T>() where T : Entity
            {
                throw new NotSupportedException();
            }

            public Task<R> ExecuteSumQueryAsync<T, R>(QueryBuilder<T> builder) where T : Entity
            {
                throw new NotSupportedException();
            }

            public Task<R?> ExecuteMaxQueryAsync<T, R>(QueryBuilder<T> builder)
                where T : Entity
                where R : struct
            {
                throw new NotSupportedException();
            }

            public Task<int> ExecuteCountQueryAsync<T>(QueryBuilder<T> builder) where T : Entity
            {
                throw new NotSupportedException();
            }

            public Task<bool> ExecuteExistsQueryAsync<T>(QueryBuilder<T> builder) where T : Entity
            {
                throw new NotSupportedException();
            }
        }

        private Dictionary<Type, ReflectionCacheItem> Cache { get; } = new Dictionary<Type, ReflectionCacheItem>();

        private ContextMock ContextMockInstance { get; } = new ContextMock();

        public ReflectionCacheItem GetCacheItem(Type type)
        {
            if (!Cache.TryGetValue(type, out var cacheItem))
            {
                throw new InvalidOperationException($"Entity class {type.FullName} is not cached.");
            }

            return cacheItem;
        }

        public void CacheAllEntityClassesInAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<EntityAttribute>(false) != null)
            )
            {
                CacheEntityClass(type);
            }
        }

        private T CreateEntityInternal<T>(Type type, IFields fields) where T : Entity
        {
            return (T)Activator.CreateInstance(type, ContextMockInstance, fields)!;
        }

        private void CacheEntityClass(Type type)
        {
            var cacheItem = new ReflectionCacheItem(type);

            var typeToInstantiate = type.GetCustomAttribute<EntityAttribute>()!.SubTypes?.FirstOrDefault() ?? type;

            var entity = CreateEntityInternal<Entity>(typeToInstantiate, new FieldsMock())!;

            foreach (var property in type.GetProperties())
            {
                if (property.PropertyType.IsGenericType &&
                    property.PropertyType.GetGenericTypeDefinition() == typeof(IRelationIdManyToIdMany<>))
                {
                    var (connectionTable, ownConnectionKey, foreignConnectionKey) = ((IRelationIdManyToIdManyInternal)property.GetValue(entity)).GetConnectionInfo();

                    cacheItem.Properties[property] = new ReflectionCacheItemPropertyManyToManyRelation(property, ownConnectionKey, foreignConnectionKey, connectionTable);
                }
                else if (property.PropertyType.IsGenericType &&
                         (
                            property.PropertyType.GetGenericTypeDefinition() == typeof(IRelationIdToFieldMany<>) ||
                            property.PropertyType.GetGenericTypeDefinition() == typeof(IRelationIdToField<>)
                         )
                )
                {
                    var foreignKey = ((IRelationIdToFieldInternal)property.GetValue(entity)).ForeignKey;

                    cacheItem.Properties[property] = new ReflectionCacheItemPropertyIdToFieldRelation(property, foreignKey);
                }
            }

            Cache[type] = cacheItem;
        }

        public void PrecacheAssemblyTypes(Assembly assembly)
        {
            CacheAllEntityClassesInAssembly(assembly);
        }
    }
}
