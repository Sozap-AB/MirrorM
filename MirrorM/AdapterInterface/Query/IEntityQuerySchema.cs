using System.Collections.Generic;

namespace MirrorM.AdapterInterface.Query
{
    public interface IEntityQuerySchema : IBaseEntityQuerySchema
    {
        IEnumerable<Join> Joins { get; }
    }
}
