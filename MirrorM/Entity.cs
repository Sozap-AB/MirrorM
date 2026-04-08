using MirrorM.Attributes;
using MirrorM.Exceptions;
using MirrorM.Internal;
using MirrorM.Internal.Relations;
using MirrorM.Relations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MirrorM
{
    public abstract class Entity
    {
        public const string FIELD_ID = "id";
        public const string FIELD_TYPE = "_type";
        public const string FIELD_VERSION = "_version";
        public const string FIELD_CREATED_AT = "_created_at";
        public const string FIELD_UPDATED_AT = "_updated_at";

        public const long DEFAULT_VERSION = 1;

        internal IFieldsInternal Fields { get; private set; }
        internal EntityState EntityState { get; private set; } = EntityState.New;
        internal long OriginalVersion { get; private set; }
        internal HashSet<string> UpdatedFields { get; } = new HashSet<string>();

        private IDictionary<string, object> RelationCache { get; } = new Dictionary<string, object>();

        internal IContext Context { get; private set; }
        internal IContextInternal ContextInternal => (IContextInternal)Context;
        protected ISuperContext? SuperContext => ContextInternal.GetSuperContext();

        public bool IsUpdated => EntityState == EntityState.Loaded && UpdatedFields.Count > 0;

        [Field(FIELD_ID)]
        public Guid Id
        {
            get => GetValue<Guid>(FIELD_ID);
            private set => SetValue(FIELD_ID, value);
        }

        [Field(FIELD_VERSION)]
        public long Version
        {
            get => GetValue<long>(FIELD_VERSION);
            private set => SetValue(FIELD_VERSION, value);
        }

        [Field(FIELD_CREATED_AT)]
        public DateTimeOffset CreatedAt
        {
            get => GetValue<DateTimeOffset>(FIELD_CREATED_AT);
            private set => SetValue(FIELD_CREATED_AT, value);
        }

        [Field(FIELD_UPDATED_AT)]
        public DateTimeOffset UpdatedAt
        {
            get => GetValue<DateTimeOffset>(FIELD_UPDATED_AT);
            private set => SetValue(FIELD_UPDATED_AT, value);
        }

        protected Entity(IContext context) : this(context, Guid.NewGuid())
        {
        }

        protected Entity(IContext context, Guid id)
        {
            Context = context;

            var now = DateTimeOffset.UtcNow;

            var collection = new Dictionary<string, object?>() {
                { FIELD_ID, id },
                { FIELD_VERSION, DEFAULT_VERSION },
                { FIELD_CREATED_AT, now },
                { FIELD_UPDATED_AT, now },
            };

            if (GetType().GetCustomAttribute<EntityAttribute>()!.SubTypes?.Contains(GetType()) ?? false)
            {
                collection[FIELD_TYPE] = GetType().Name;
            }

            Fields = new FieldCollection(collection);

            OriginalVersion = DEFAULT_VERSION;

            ContextInternal.Add(this);
        }

        protected Entity(IContext context, IFields fields)
        {
            Context = context;

            Fields = (IFieldsInternal)fields;

            OriginalVersion = Fields.GetValue<long>(FIELD_VERSION);

            EntityState = EntityState.Loaded;

            ContextInternal.Add(this);
        }

        protected internal T GetValue<T>(string field)
        {
            return Fields.GetValue<T>(field);
        }

        protected internal void SetValue<T>(string field, T value)
        {
            Fields.SetValue(field, value);

            OnFieldUpdated(field);
        }

        public void Delete()
        {
            switch (EntityState)
            {
                case EntityState.New:
                    EntityState = EntityState.Ignored;
                    break;
                default:
                    EntityState = EntityState.Deleted;
                    break;
            }
        }

        private void OnFieldUpdated(string field)
        {
            var isEntityJustUpdated = UpdatedFields.Count == 0;

            UpdatedFields.Add(field);

            if (isEntityJustUpdated && EntityState == EntityState.Loaded)
            {
                Version++;
                UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        protected IRelationIdToField<T> GetRelationIdToField<T>(Expression<Func<T, Guid>> keyProvider) where T : Entity
        {
            var field = GetProperty(keyProvider).GetCustomAttribute<FieldAttribute>()?.FieldName ?? throw new FieldAttributeNotFoundException();

            var key = $"RelationIdToField:{typeof(T).Name}:{field}";

            if (RelationCache.TryGetValue(key, out var relation))
            {
                return (IRelationIdToField<T>)relation;
            }

            RelationCache[key] = new RelationIdToField<T>(this, keyProvider);

            return (IRelationIdToField<T>)RelationCache[key];
        }

        protected IRelationFieldToId<T> GetRelationFieldToId<R, T>(Expression<Func<R, Guid?>> keyProvider) where T : Entity where R : Entity
        {
            return GetRelationFieldToIdInternal<T>(keyProvider);
        }

        protected IRelationFieldToId<T> GetRelationFieldToId<R, T>(Expression<Func<R, Guid>> keyProvider) where T : Entity where R : Entity
        {
            return GetRelationFieldToIdInternal<T>(keyProvider);
        }

        private IRelationFieldToId<T> GetRelationFieldToIdInternal<T>(Expression keyProvider) where T : Entity
        {
            var field = GetProperty(keyProvider).GetCustomAttribute<FieldAttribute>()?.FieldName ?? throw new FieldNameNotFoundException();

            var key = $"RelationFieldToId:{typeof(T).Name}:{field}";

            if (RelationCache.TryGetValue(key, out var relation))
            {
                return (IRelationFieldToId<T>)relation;
            }

            RelationCache[key] = new RelationFieldToId<T>(this, field);

            return (IRelationFieldToId<T>)RelationCache[key];
        }

        protected IRelationIdToFieldMany<T> GetRelationIdToFieldMany<T>(Expression<Func<T, Guid>> keyProvider) where T : Entity
        {
            var field = GetProperty(keyProvider).GetCustomAttribute<FieldAttribute>()?.FieldName ?? throw new FieldNameNotFoundException();

            var key = $"RelationIdToFieldMany:{typeof(T).Name}:{field}";

            if (RelationCache.TryGetValue(key, out var relation))
            {
                return (IRelationIdToFieldMany<T>)relation;
            }

            RelationCache[key] = new RelationIdToFieldMany<T>(this, keyProvider);

            return (IRelationIdToFieldMany<T>)RelationCache[key];
        }

        protected IRelationIdManyToIdMany<T> GetRelationManyToManyForeign<T>(string connectionTable, string connectionKey, string foreignConnectionKey) where T : Entity
        {
            var key = $"RelationIdManyToIdMany:{typeof(T).Name}:{connectionTable}";

            if (RelationCache.TryGetValue(key, out var relation))
            {
                return (IRelationIdManyToIdMany<T>)relation;
            }

            RelationCache[key] = new RelationIdManyToIdMany<T>(this, connectionTable, connectionKey, foreignConnectionKey);

            return (RelationIdManyToIdMany<T>)RelationCache[key];
        }

        private static PropertyInfo GetProperty(
            Expression selector
        )
        {
            Expression body = selector;
            if (body is LambdaExpression expression)
            {
                body = expression.Body;
            }
            switch (body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return (PropertyInfo)((MemberExpression)body).Member;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal IReadOnlyDictionary<string, object?> GetModifications()
        {
            return Fields.GetEnumerable().Where(f => UpdatedFields.Contains(f.Key)).ToDictionary(f => f.Key, f => f.Value);
        }

        internal void UpdateFromCommited(Entity justSavedEntity)
        {
            if (!IsUpdated)
            {
                Fields.CopyFields(justSavedEntity.Fields);

                return;
            }

            if (UpdatedFields.Where(f => f != FIELD_VERSION && f != FIELD_UPDATED_AT).Intersect(justSavedEntity.UpdatedFields).Any())
            {
                throw new TransactionEntityUpdateConflictException(); //TODO: add details to the exception
            }

            var oldUpdatedAt = UpdatedAt;

            Fields.CopyFields(justSavedEntity.Fields, justSavedEntity.UpdatedFields);
            OriginalVersion = Version;

            Version++;
            UpdatedAt = oldUpdatedAt;
        }
    }
}
