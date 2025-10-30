using System;
using System.Collections.Generic;
using System.Linq;

namespace MirrorM.Internal.Query.Storage
{
    internal class ConnectionTableStorage
    {
        private sealed class ConnectionEquatyComparer : IEqualityComparer<Connection>
        {
            public bool Equals(Connection x, Connection y)
            {
                return x.Key1Value == y.Key1Value && x.Key2Value == y.Key2Value;
            }
            public int GetHashCode(Connection obj)
            {
                return HashCode.Combine(obj.Key1Value, obj.Key2Value);
            }
        }

        public ISet<Connection> Connections { get; }

        public string Table { get; }
        public string Key1 { get; }
        public string Key2 { get; }

        public ConnectionTableStorage(string table, string key1, string key2)
        {
            Table = table;
            Key1 = key1;
            Key2 = key2;
            Connections = new HashSet<Connection>(new ConnectionEquatyComparer());
        }

        public void AddConnection((string key, Guid value) kv1, (string key, Guid value) kv2, EntityState state)
        {
            if (kv1.key != Key1)
                (kv1, kv2) = (kv2, kv1);

            if (Connections.Any(c => c.Key1Value == kv1.value && c.Key2Value == kv2.value))
                return;

            Connections.Add(new Connection(kv1.value, kv2.value, state));
        }

        public void MarkConnectionRemoved((string key, Guid value) kv1, (string key, Guid value) kv2)
        {
            if (kv1.key != Key1)
                (kv1, kv2) = (kv2, kv1);

            var connection = Connections.FirstOrDefault(c => c.Key1Value == kv1.value && c.Key2Value == kv2.value);

            if (connection == null)
            {
                Connections.Add(new Connection(kv1.value, kv2.value, EntityState.Deleted));
            }
            else if (connection.State == EntityState.New)
            {
                connection.State = EntityState.Ignored;
            }
            else
            {
                connection.State = EntityState.Deleted;
            }
        }

        public bool IsEntityConnected((string key, Guid value) kv1, (string key, Guid value) kv2)
        {
            if (kv1.key != Key1)
                (kv1, kv2) = (kv2, kv1);

            return Connections.Any(c => c.Key1Value == kv1.value && c.Key2Value == kv2.value);
        }
    }
}
