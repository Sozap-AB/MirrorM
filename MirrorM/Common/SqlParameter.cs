namespace MirrorM.Common
{
    public class SqlParameter
    {
        public string Name { get; set; }
        public object? Value { get; set; }

        public SqlParameter(string name, object? value)
        {
            Name = name;
            Value = value;
        }
    }
}
