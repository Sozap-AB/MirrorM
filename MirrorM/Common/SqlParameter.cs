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

        public SqlParameter(string name, object value) : this(name, new SqlParameterValue(value))
        {
        }
    }
}
