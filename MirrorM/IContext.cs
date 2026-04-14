using MirrorM.Common;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace MirrorM
{
    public interface IContext : IDisposable
    {
        IQuery<T> Query<T>() where T : EntityBase;

        IAsyncEnumerable<T> ExecuteSqlQueryAsync<T>(string sql, params SqlParameter[] parameters) where T : EntityBase;
        IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ExecuteSqlQueryAndGetRawResultAsync(string sql, params SqlParameter[] parameters);
        Task<int> ExecuteSqlCommandAsync(string sql, params SqlParameter[] parameters);

        Task ExecuteTransactionAsync(Func<Task> action);

        Task InitializeAsync();
        Task CommitAsync();

        void SetSuperContext(ISuperContext? superContext);
        void SetSqlInterceptor(Action<string, IEnumerable<DbParameter>>? sqlInterceptor);
    }
}
