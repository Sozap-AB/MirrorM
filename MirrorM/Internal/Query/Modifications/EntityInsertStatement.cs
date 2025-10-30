using MirrorM.AdapterInterface.Query;
using System;
using System.Collections.Generic;

namespace MirrorM.Internal.Query.Modifications
{
    internal class EntityInsertStatement : EntityModifyStatement, IEntityInsertSchema
    {
        public IReadOnlyDictionary<string, object?> Fields { get; }

        public EntityInsertStatement(Type entityType, IReadOnlyDictionary<string, object?> fields) : base(entityType)
        {
            Fields = fields;
        }
    }
}
