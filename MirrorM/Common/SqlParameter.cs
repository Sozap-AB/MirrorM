namespace MirrorM.Common
{
    public class SqlParameter
    {
        public string Name { get; set; }
        public SqlParameterValue Value { get; set; }

        public SqlParameter(string name, SqlParameterValue value)
        {
            Name = name;
            Value = value;
        }
    }
}
