using MirrorM.AdapterInterface.Query;
using MirrorM.AdapterInterface.Query.Conditions;
using System;
using System.Collections.Generic;

namespace MirrorM.Internal.Query.Modifications
{
    internal class EntityDeleteStatement : EntityModifyStatement, IEntityDeleteSchema
    {
        public IEnumerable<ExpressionBase> Conditions { get; }

        public EntityDeleteStatement(Type entityType, IEnumerable<ExpressionBase> conditions) : base(entityType)
        {
            Conditions = conditions;
        }
    }
}
