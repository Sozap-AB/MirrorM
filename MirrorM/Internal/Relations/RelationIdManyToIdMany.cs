using MirrorM.Internal.Query.Builder;
using MirrorM.Relations;

namespace MirrorM.Internal.Relations
{
    internal class RelationIdManyToIdMany<T> : IRelationIdManyToIdMany<T>, IRelationIdManyToIdManyInternal where T : Entity
    {
        private Entity Owner { get; }
        private string ConnectionTable { get; }
        private string OwnerConnectionKey { get; }
        private string ForeignConnectionKey { get; }

        public RelationIdManyToIdMany(Entity owner, string connectionTable, string ownerConnectionKey, string foreignConnectionKey)
        {
            Owner = owner;
            ConnectionTable = connectionTable;
            OwnerConnectionKey = ownerConnectionKey;
            ForeignConnectionKey = foreignConnectionKey;
        }

        public void Attach(T entity)
        {
            Owner.ContextInternal.AddConnection(ConnectionTable, OwnerConnectionKey, Owner.Id, ForeignConnectionKey, entity.Id);
        }

        public void Detach(T entity)
        {
            Owner.ContextInternal.RemoveConnection(ConnectionTable, OwnerConnectionKey, Owner.Id, ForeignConnectionKey, entity.Id);
        }

        public IQuery<T> Query()
        {
            return ((QueryBuilder<T>)Owner.Context.Query<T>()).WhereConnectionForeignMatch(ConnectionTable, ForeignConnectionKey, OwnerConnectionKey, Owner.Id);
        }

        public (string, string, string) GetConnectionInfo()
        {
            return (ConnectionTable, OwnerConnectionKey, ForeignConnectionKey);
        }
    }
}
