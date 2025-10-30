using MirrorM.AdapterInterface.Query;
using MirrorM.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace MirrorM.AdapterInterface
{
    public interface IDatabaseConnection : IDisposable
    {
        IAsyncEnumerable<T> ExecuteSelectAsync<T>(IEntityQuerySchema schema, Func<IDataReader, IEnumerable<T>> recordHandler);

        Task<R> ExecuteSumAsync<R>(IEntityAggregateQuerySchema schema);
        Task<R?> ExecuteMaxAsync<R>(IEntityAggregateQuerySchema schema) where R : struct;
        Task<int> ExecuteCountAsync(IEntityAggregateQuerySchema schema);
        Task<bool> ExecuteExistsAsync(IEntityAggregateQuerySchema schema);

        Task ExecuteCommandBatchAsync(IEnumerable<IModificationSchema> expressions, bool createTransaction);

        IAsyncEnumerable<T> ExecuteRawSelectAsync<T>(string sql, SqlParameter[] parameters, Func<IDataReader, T> recordHandler);
        Task<int> ExecuteRawCommandAsync(string sql, SqlParameter[] parameters);

        Task ExecuteWrappedInTransactionAsync(Func<Task> action);
    }
}
