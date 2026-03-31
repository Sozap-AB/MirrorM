using MirrorM.AdapterInterface.Query.Conditions;
using MirrorM.Common;
using System.Collections.Generic;

namespace MirrorM.AdapterInterface.Query
{
    public interface IEntityUpdateSchema : IEntityModificationSchema
    {
        IEnumerable<ExpressionBase> Conditions { get; }
        IEnumerable<SqlParameter> Fields { get; }
    }
}
