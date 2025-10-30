using MirrorM.AdapterInterface.Query;
using MirrorM.AdapterInterface.Query.Conditions;
using System;
using System.Collections.Generic;

namespace MirrorM.Internal.Query.Modifications
{
    internal class EntityUpdateStatement : EntityModifyStatement, IEntityUpdateSchema
    {
        public IEnumerable<ExpressionBase> Conditions { get; }

        public IReadOnlyDictionary<string, object?> Fields { get; }

        public EntityUpdateStatement(Type entityType, IEnumerable<ExpressionBase> conditions, IReadOnlyDictionary<string, object?> fields) : base(entityType)
        {
            Conditions = conditions;
            Fields = fields;
        }
    }
}
