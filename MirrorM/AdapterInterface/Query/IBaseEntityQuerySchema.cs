using MirrorM.AdapterInterface.Query.Conditions;
using System;
using System.Collections.Generic;

namespace MirrorM.AdapterInterface.Query
{
    public interface IBaseEntityQuerySchema
    {
        IEnumerable<ExpressionBase> Conditions { get; }
        IEnumerable<Sorting> Sortings { get; }
        int SkipCount { get; }
        int? TakeCount { get; }
        Type EntityType { get; }
    }
}
