using MirrorM.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MirrorM
{
    public interface IContext : IDisposable
    {
        IQuery<T> Query<T>() where T : Entity;

        IAsyncEnumerable<T> ExecuteSqlQueryAsync<T>(string sql, params SqlParameter[] parameters) where T : Entity;
        IAsyncEnumerable<IReadOnlyDictionary<string, object>> ExecuteSqlQueryAndGetRawResultAsync(string sql, params SqlParameter[] parameters);
        Task<int> ExecuteSqlCommandAsync(string sql, params SqlParameter[] parameters);

        Task ExecuteTransactionAsync(Func<Task> action);

        Task CommitAsync();
    }
}
