using System;
using System.Collections.Generic;

namespace MirrorM.Internal.Common
{
    internal static class CollectionExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T, int> action)
        {
            var i = 0;
            foreach (var e in ie) action(e, i++);
        }
    }
}
