using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MirrorM.Internal.Common
{
    internal static class CollectionExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T, int> action)
        {
            var i = 0;
            foreach (var e in ie) action(e, i++);
        }

        public static async Task<T?> SingleOrDefaultAsync<T>(this IAsyncEnumerable<T> ae) where T : class
        {
            await using var enumerator = ae.GetAsyncEnumerator();

            if (await enumerator.MoveNextAsync())
            {
                var result = enumerator.Current;

                if (await enumerator.MoveNextAsync())
                    throw new InvalidOperationException("Sequence contains more than one element.");

                return result;
            }

            return default;
        }
    }
}
