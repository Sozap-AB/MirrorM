using MirrorM.AdapterInterface.Query.Connections;
using System;

namespace MirrorM.Internal.Query.Modifications.Connections
{
    internal class ConnectionModifyStatement : IConnectionModificationSchema
    {
        public string Table { get; }
        public string Key1 { get; }
        public Guid Value1 { get; }
        public string Key2 { get; }
        public Guid Value2 { get; }

        public ConnectionModifyStatement(string table, string key1, Guid value1, string key2, Guid value2)
        {
            Table = table;
            Key1 = key1;
            Value1 = value1;
            Key2 = key2;
            Value2 = value2;
        }
    }
}
