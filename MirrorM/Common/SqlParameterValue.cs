using System;

namespace MirrorM.Common
{
    public class SqlParameterValue
    {
        public object Value { get; }
        public SqlFieldType? SqlType { get; }

        public static SqlParameterValue Empty { get; } = new SqlParameterValue(DBNull.Value, null);

        public SqlParameterValue(object value, SqlFieldType? sqlType)
        {
            Value = value;
            SqlType = sqlType;
        }

        public SqlParameterValue(object value) : this(value, null)
        {
        }

        public override bool Equals(object obj)
        {
            if (obj is SqlParameterValue spv)
                return Value.Equals(spv.Value) && SqlType.Equals(spv.SqlType);

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, SqlType);
        }
    }
}
