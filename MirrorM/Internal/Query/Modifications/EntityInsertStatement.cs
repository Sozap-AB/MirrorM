using MirrorM.AdapterInterface.Query;
using MirrorM.Common;
using System;
using System.Collections.Generic;

namespace MirrorM.Internal.Query.Modifications
{
    internal class EntityInsertStatement : EntityModifyStatement, IEntityInsertSchema
    {
        public IEnumerable<SqlParameter> Fields { get; }

        public EntityInsertStatement(Type entityType, IEnumerable<SqlParameter> fields) : base(entityType)
        {
            Fields = fields;
        }
    }
}
