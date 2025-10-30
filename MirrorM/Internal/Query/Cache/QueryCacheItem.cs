using MirrorM.AdapterInterface.Query.Conditions;
using System.Collections.Generic;
using System.Linq;

namespace MirrorM.Internal.Query.Cache
{
    internal class QueryCacheItem
    {
        private IEnumerable<ExpressionBase> Conditions { get; }

        public QueryCacheItem(IEnumerable<ExpressionBase> conditions)
        {
            Conditions = conditions;
        }

        public bool IsCoveringCacheItem(QueryCacheItem otherItem)
        {
            return Conditions.All(myCondition => otherItem.Conditions.Any(otherCondition => myCondition.Equals(otherCondition)));
        }
    }
}
