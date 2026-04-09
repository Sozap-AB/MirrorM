using System;

namespace MirrorM.AdapterInterface.Query.Conditions
{
    public class ExpressionNot : ExpressionBase
    {
        public ExpressionBase Operand { get; }

        internal ExpressionNot(ExpressionBase operand)
        {
            Operand = operand;
        }

        public override bool Equals(object obj)
        {
            if (obj is ExpressionNot en)
                return en.Operand.Equals(Operand);

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Operand);
        }
    }
}
