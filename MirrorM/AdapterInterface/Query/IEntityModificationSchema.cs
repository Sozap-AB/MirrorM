using System;

namespace MirrorM.AdapterInterface.Query
{
    public interface IEntityModificationSchema : IModificationSchema
    {
        Type EntityType { get; }
    }
}
