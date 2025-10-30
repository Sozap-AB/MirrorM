using FishingTourServer.Sys.Services.Data.Database.Attributes;
using MirrorM.Exceptions;
using MirrorM.Internal.Common;
using MirrorM.Relations;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MirrorM.Internal.Relations
{
    internal class RelationIdToFieldMany<T> : IRelationIdToFieldMany<T>, IRelationIdToFieldInternal where T : Entity
    {
        private Entity Owner { get; }
        private Expression<Func<T, Guid>> FieldKeyProvider { get; }
        public string ForeignKey { get; }

        public RelationIdToFieldMany(Entity owner, Expression<Func<T, Guid>> foreignKeyProvider)
        {
            Owner = owner;
            FieldKeyProvider = foreignKeyProvider;
            ForeignKey = ReflectionTools.GetPropertyFromSelector(foreignKeyProvider).GetCustomAttribute<FieldAttribute>()?.FieldName ?? throw new FieldNameNotFoundException();
        }

        public void AttachTo(T entity)
        {
            entity.SetValue(ForeignKey, Owner.Id);
        }

        public void DetachFrom(T entity)
        {
            entity.SetValue<Guid?>(ForeignKey, null);
        }

        public IQuery<T> Query()
        {
            var idExpression = FieldKeyProvider.Body.Type == typeof(Guid) ? Expression.Constant(Owner.Id) : Expression.Constant(Owner.Id, typeof(Guid?));

            //TODO: we probably don't need to build expression, we can make internal method that accepts field name and value

            return Owner.Context.Query<T>().Where(
                Expression.Lambda<Func<T, bool>>(Expression.Equal(FieldKeyProvider.Body, idExpression), FieldKeyProvider.Parameters[0])
            );
        }
    }
}
