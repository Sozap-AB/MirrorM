using MirrorM.Internal.Common;
using MirrorM.Relations;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MirrorM.Internal.Relations
{
    internal class RelationIdToField<T> : IRelationIdToField<T>, IRelationIdToFieldInternal where T : Entity
    {
        private Entity Owner { get; }
        private Expression<Func<T, Guid>> FieldProvider { get; } //TODO: check if needed
        public string ForeignKey { get; }

        public RelationIdToField(Entity owner, Expression<Func<T, Guid>> fieldProvider)
        {
            Owner = owner;
            FieldProvider = fieldProvider;
            ForeignKey = ReflectionTools.GetFieldNameFromSelector(fieldProvider);
        }

        public void AttachTo(T entity)
        {
            entity.SetValue(ForeignKey, Owner.Id);
        }

        public async Task<T> GetAsync()
        {
            return (await GetOptionalAsync())!;
        }

        public Task<T?> GetOptionalAsync()
        {
            var idExpression = FieldProvider.Body.Type == typeof(Guid) ? Expression.Constant(Owner.Id) : Expression.Constant(Owner.Id, typeof(Guid?));

            //TODO: we probably don't need to build expression, we can make internal method that accepts field name and value

            return Owner.Context.Query<T>().FirstOrDefaultAsync(
                Expression.Lambda<Func<T, bool>>(Expression.Equal(FieldProvider.Body, idExpression), FieldProvider.Parameters[0])
            );
        }
    }
}
