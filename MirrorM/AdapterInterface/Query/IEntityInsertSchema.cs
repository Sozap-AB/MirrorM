using System.Collections.Generic;

namespace MirrorM.AdapterInterface.Query
{
    public interface IEntityInsertSchema : IEntityModificationSchema
    {
        IReadOnlyDictionary<string, object> Fields { get; }
    }
}
