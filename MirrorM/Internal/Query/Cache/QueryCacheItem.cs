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
        private IEnumerable<Sorting> Sortings { get; }
        private int? TakeCount { get; }

        public QueryCacheItem(IEntityQuerySchema schema)
        {
            EntityType = schema.EntityType;
            Conditions = schema.Conditions;
            TakeCount = schema.TakeCount;
            Sortings = schema.Sortings;
        }

        public bool IsCoveringCacheItem(QueryCacheItem otherItem)
        {
            if (EntityType != otherItem.EntityType)
                return false;

            if (otherItem.TakeCount.HasValue && TakeCount.HasValue)
            {
                return TakeCount.Value >= otherItem.TakeCount.Value
                        && Conditions.SequenceEqual(otherItem.Conditions)
                        && Sortings.SequenceEqual(otherItem.Sortings);
            }

            if (TakeCount.HasValue)
                return false;

            return Conditions.All(myCondition => otherItem.Conditions.Any(otherCondition => myCondition.Equals(otherCondition)));
        }
    }
}
