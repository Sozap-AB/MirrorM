using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MirrorM
{
    public interface IQuery<T> where T : Entity
    {
        IQuery<T> Where(Expression<Func<T, bool>> predicate);
        IQuery<T> WhereRawSQL(string sql);

        IQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> fieldSelector);
        IQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> fieldSelector);

        IQuery<T> Take(int count);
        IQuery<T> Skip(int count);

        Task<R> SumAsync<R>(Expression<Func<T, R>> fieldSelector);
        Task<R?> MaxAsync<R>(Expression<Func<T, R>> fieldSelector) where R : struct;
        Task<int> CountAsync();
        Task<bool> AnyAsync();

        IAsyncEnumerable<T> ToAsyncEnumerable();

        Task DeleteAsync(); // Not implemented
        Task UpdateAsync(Expression<Func<T>> updater); // Not implemented
    }
}
