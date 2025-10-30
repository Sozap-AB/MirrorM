using System;

namespace MirrorM.AdapterInterface.Query.Connections
{
    public interface IConnectionModificationSchema : IModificationSchema
    {
        public string Table { get; }
        public string Key1 { get; }
        public Guid Value1 { get; }
        public string Key2 { get; }
        public Guid Value2 { get; }
    }
}
