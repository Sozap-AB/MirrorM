using MirrorM.AdapterInterface.Query.Conditions;
using System.Collections.Generic;

namespace MirrorM.AdapterInterface.Query
{
    public interface IEntityUpdateSchema : IEntityModificationSchema
    {
        IEnumerable<ExpressionBase> Conditions { get; }
        IReadOnlyDictionary<string, object> Fields { get; }
    }
}
