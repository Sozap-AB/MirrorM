using MirrorM.AdapterInterface.Query.Connections;
using System;

namespace MirrorM.Internal.Query.Modifications.Connections
{
    internal class ConnectionInsertStatement : ConnectionModifyStatement, IConnectionInsertSchema
    {
        public ConnectionInsertStatement(string table, string key1, Guid value1, string key2, Guid value2) : base(table, key1, value1, key2, value2)
        {
        }
    }
}
