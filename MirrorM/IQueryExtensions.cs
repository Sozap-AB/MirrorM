using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MirrorM
{
    public static class IQueryExtensions
    {
        public static async Task<T> FirstAsync<T>(this IQuery<T> query) where T : Entity
        {
            return await query.Take(1).FirstOrDefaultAsync() ?? throw new InvalidOperationException();
        }

        public static async Task<T> FirstAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate) where T : Entity
        {
            return await query.Where(predicate).FirstAsync();
        }

        public static async Task<T?> FirstOrDefaultAsync<T>(this IQuery<T> query) where T : Entity
        {
            await using var enumerator = query.Take(1).ToAsyncEnumerable().GetAsyncEnumerator();

            if (await enumerator.MoveNextAsync())
                return enumerator.Current;

            return null;
        }

        public static async Task<T?> FirstOrDefaultAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate) where T : Entity
        {
            return await query.Where(predicate).FirstOrDefaultAsync();
        }

        public static async Task<IList<T>> ToListAsync<T>(this IQuery<T> query) where T : Entity
        {
            var result = new List<T>(); //TODO: use information from query to preallocate the list

            await foreach (var item in query.ToAsyncEnumerable())
            {
                result.Add(item);
            }

            return result;
        }
    }
}
