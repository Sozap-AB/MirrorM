using MirrorM.AdapterInterface.Query;
using MirrorM.AdapterInterface.Query.Conditions;
using MirrorM.Common;
using System;
using System.Collections.Generic;

namespace MirrorM.Internal.Query.Modifications
{
    internal class EntityUpdateStatement : EntityModifyStatement, IEntityUpdateSchema
    {
        public IEnumerable<ExpressionBase> Conditions { get; }

        public IEnumerable<SqlParameter> Fields { get; }

        public EntityUpdateStatement(
            Type entityType,
            IEnumerable<ExpressionBase> conditions,
            IEnumerable<SqlParameter> fields
        ) : base(entityType)
        {
            Conditions = conditions;
            Fields = fields;
        }
    }
}
