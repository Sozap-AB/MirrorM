namespace MirrorM.AdapterInterface.Query.Conditions
{
    public class ExpressionField : ExpressionBase
    {
        public string FieldName { get; }

        internal ExpressionField(string fieldName)
        {
            FieldName = fieldName;
        }

        public override bool Equals(object obj)
        {
            if (obj is ExpressionField ef)
                return FieldName.Equals(ef.FieldName);

            return false;
        }
    }
}
