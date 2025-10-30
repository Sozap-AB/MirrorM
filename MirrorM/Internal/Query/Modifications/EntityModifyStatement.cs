using System;

namespace MirrorM.Internal.Query.Modifications
{
    internal class EntityModifyStatement
    {
        public Type EntityType { get; }

        public EntityModifyStatement(Type entityType)
        {
            EntityType = entityType;
        }
    }
}
