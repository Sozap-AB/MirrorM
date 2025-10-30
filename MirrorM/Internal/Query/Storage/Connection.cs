using System;

namespace MirrorM.Internal.Query.Storage
{
    internal class Connection
    {
        public Guid Key1Value { get; }
        public Guid Key2Value { get; }

        public EntityState State { get; internal set; }

        public Connection(Guid key1Value, Guid key2Value, EntityState state)
        {
            Key1Value = key1Value;
            Key2Value = key2Value;
            State = state;
        }
    }
}
