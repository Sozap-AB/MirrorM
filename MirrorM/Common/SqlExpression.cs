using System.Collections.Generic;

namespace MirrorM.Common
{
    public class SqlExpression
    {
        public string SqlString { get; }
        public IEnumerable<SqlParameter> Parameters { get; }

        public SqlExpression(string sqlString, IEnumerable<SqlParameter> parameters)
        {
            SqlString = sqlString;
            Parameters = parameters;
        }
    }
}
