using MirrorM.AdapterInterface.Query;
using MirrorM.AdapterInterface.Query.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MirrorM.Internal.Query.Cache
{
    internal class QueryCacheItem
    {
        private Type EntityType { get; }
        private IEnumerable<ExpressionBase> Conditions { get; }

        public QueryCacheItem(IEntityQuerySchema schema)
        {
            EntityType = schema.EntityType;
            Conditions = schema.Conditions;
        }

        public bool IsCoveringCacheItem(QueryCacheItem otherItem)
        {
            if (EntityType != otherItem.EntityType)
                return false;

            return Conditions.All(myCondition => otherItem.Conditions.Any(otherCondition => myCondition.Equals(otherCondition)));
        }
    }
}
