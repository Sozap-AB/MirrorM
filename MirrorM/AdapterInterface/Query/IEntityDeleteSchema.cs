using MirrorM.AdapterInterface.Query.Conditions;
using System.Collections.Generic;

namespace MirrorM.AdapterInterface.Query
{
    public interface IEntityDeleteSchema : IEntityModificationSchema
    {
        IEnumerable<ExpressionBase> Conditions { get; }
    }
}
