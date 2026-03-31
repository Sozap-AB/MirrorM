using MirrorM.Common;
using System.Collections.Generic;

namespace MirrorM.AdapterInterface.Query
{
    public interface IEntityInsertSchema : IEntityModificationSchema
    {
        IEnumerable<SqlParameter> Fields { get; }
    }
}
