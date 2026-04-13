using MirrorM.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MirrorM.AdapterInterface.Query.Conditions
{
    public class ExpressionRawSql : ExpressionBase
    {
        public string Sql { get; }
        public IEnumerable<SqlParameter> Parameters { get; }

        internal ExpressionRawSql(string sql, IEnumerable<SqlParameter> parameters)
        {
            this.Sql = sql;
            this.Parameters = parameters;
        }

        public override bool Equals(object obj)
        {
            if (obj is ExpressionRawSql ec)
                return Sql.Equals(ec.Sql) && Parameters.SequenceEqual(ec.Parameters);

            return false;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();

            hash.Add(Sql);

            foreach (var param in Parameters)
                hash.Add(param);

            return hash.ToHashCode();
        }
    }
}
