using MirrorM.Internal.Common;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MirrorM
{
    public static class IQueryExtensions
    {
        public static async Task<T> FirstAsync<T>(this IQuery<T> query) where T : EntityBase
        {
            return await query.Take(1).FirstOrDefaultAsync() ?? throw new InvalidOperationException("Sequence contains no items.");
        }

        public static async Task<T> FirstAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate) where T : EntityBase
        {
            return await query.Where(predicate).FirstAsync();
        }

        public static async Task<T?> FirstOrDefaultAsync<T>(this IQuery<T> query) where T : EntityBase
        {
            return await query.Take(1).ToAsyncEnumerable().SingleOrDefaultAsync();
        }

        public static Task<bool> AnyAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate) where T : EntityBase
        {
            return query.Where(predicate).AnyAsync();
        }

        public static async Task<T?> FirstOrDefaultAsync<T>(this IQuery<T> query, Expression<Func<T, bool>> predicate) where T : EntityBase
        {
            return await query.Where(predicate).FirstOrDefaultAsync();
        }

        public static async Task<IList<T>> ToListAsync<T>(this IQuery<T> query) where T : EntityBase
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
