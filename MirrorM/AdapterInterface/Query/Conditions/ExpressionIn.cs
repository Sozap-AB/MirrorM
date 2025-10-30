using System.Collections.Generic;
using System.Linq;

namespace MirrorM.AdapterInterface.Query.Conditions
{
    public class ExpressionIn : ExpressionBase
    {
        public IEnumerable<object> Values { get; }

        internal ExpressionIn(IEnumerable<object> values)
        {
            Values = values;
        }

        public override bool Equals(object obj)
        {
            if (obj is ExpressionIn ei)
                return Enumerable.SequenceEqual(Values, ei.Values);

            return false;
        }
    }
}
