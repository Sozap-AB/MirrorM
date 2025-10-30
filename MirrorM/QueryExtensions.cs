using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MirrorM
{
    public static class QueryExtensions
    {
        public static async Task<T> FirstAsync<T>(this IQuery<T> query) where T : Entity
        {
            return (await query.Take(1).ToArrayAsync())[0];
        }

        public static async Task<T> FirstAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate) where T : Entity
        {
            return await query.Where(predicate).FirstAsync();
        }

        public static async Task<T?> FirstOrDefaultAsync<T>(this IQuery<T> query) where T : Entity
        {
            return (await query.Take(1).ToArrayAsync()).FirstOrDefault();
        }

        public static async Task<T?> FirstOrDefaultAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate) where T : Entity
        {
            return await query.Where(predicate).FirstOrDefaultAsync();
        }

        public static async Task<T[]> ToArrayAsync<T>(this IQuery<T> query) where T : Entity
        {
            return (await query.ToListAsync()).ToArray();
        }

        public static async Task<T[]> ToListAsync<T>(this IQuery<T> query) where T : Entity
        {
            var result = new List<T>(); //TODO: use information from query to preallocate the list

            await foreach (var item in query.ToAsyncEnumerable())
            {
                result.Add(item);
            }

            return result.ToArray();
        }
    }
}
