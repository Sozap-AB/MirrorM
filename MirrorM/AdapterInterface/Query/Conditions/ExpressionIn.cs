using System;
using System.Collections.Generic;
using System.Linq;

namespace MirrorM.AdapterInterface.Query.Conditions
{
    public class ExpressionIn : ExpressionBase
    {
        public ExpressionBase Value { get; }
        public IEnumerable<object> Collection { get; }

        internal ExpressionIn(ExpressionBase value, IEnumerable<object> collection)
        {
            this.Value = value;
            this.Collection = collection;
        }

        public override bool Equals(object obj)
        {
            if (obj is ExpressionIn ei)
                return Value.Equals(ei.Value) && Enumerable.SequenceEqual(Collection, ei.Collection);

            return false;
        }

        public override int GetHashCode()
        {
            var result = new HashCode();

            result.Add(Value);

            foreach (var item in Collection)
                result.Add(item);

            return result.ToHashCode();
        }
    }
}
