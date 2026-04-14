using MirrorM.Relations;
using System;
using System.Threading.Tasks;

namespace MirrorM.Internal.Relations
{
    internal class RelationFieldToId<T> : IRelationFieldToId<T> where T : EntityBase
    {
        private EntityBase Owner { get; }
        private string ForeignKey { get; }

        public RelationFieldToId(EntityBase owner, string foreignKey)
        {
            Owner = owner;
            ForeignKey = foreignKey;
        }

        public void AttachTo(T entity)
        {
            Owner.SetValue(ForeignKey, entity.Id);
        }

        public async Task<T> GetAsync()
        {
            return (await GetOptionalAsync())!;
        }

        public Task<T?> GetOptionalAsync()
        {
            var id = Owner.GetValue<Guid?>(ForeignKey);

            if (!id.HasValue)
                return Task.FromResult<T?>(null);

            return Owner.Context.Query<T>().FirstOrDefaultAsync(x => x.Id == id.Value);
        }
    }
}
