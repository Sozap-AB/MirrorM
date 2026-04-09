using System;

namespace MirrorM.AdapterInterface.Query.Conditions
{
    public class ExpressionBinary : ExpressionBase
    {
        public enum Operation
        {
            And,
            Or,
            Add,
            Subtract,
            Multiply,
            Divide,
            Equal,
            NotEqual,
            GreaterThan,
            GreaterThanOrEqual,
            LessThan,
            LessThanOrEqual,
        }

        public Operation Op { get; }
        public ExpressionBase Left { get; }
        public ExpressionBase Right { get; }

        internal ExpressionBinary(Operation op, ExpressionBase left, ExpressionBase right)
        {
            Op = op;
            Left = left;
            Right = right;
        }

        public override bool Equals(object obj)
        {
            if (obj is ExpressionBinary eb)
                return eb.Left.Equals(Left) && eb.Right.Equals(Right) && eb.Op == Op;

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Op, Left, Right);
        }
    }
}
