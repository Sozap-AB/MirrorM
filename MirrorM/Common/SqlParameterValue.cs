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
    }
}
