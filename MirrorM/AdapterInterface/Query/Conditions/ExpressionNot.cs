namespace MirrorM.AdapterInterface.Query.Conditions
{
    public class ExpressionNot : ExpressionBase
    {
        public ExpressionBase Operand { get; }

        internal ExpressionNot(ExpressionBase operand)
        {
            Operand = operand;
        }
    }
}
