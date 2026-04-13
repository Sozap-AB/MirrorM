using System;

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

        public override bool Equals(object obj)
        {
            if (obj is SqlParameter sp)
                return Name.Equals(sp.Name) && Value.Equals(sp.Value);

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Value);
        }
    }
}