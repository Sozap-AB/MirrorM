namespace MirrorM.AdapterInterface.Query.Conditions
{
    public class ExpressionConst : ExpressionBase
    {
        public object Value { get; }

        internal ExpressionConst(object value)
        {
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj is ExpressionConst ec)
                return Value.Equals(ec.Value);

            return false;
        }
    }
}
