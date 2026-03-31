using MirrorM.Internal.Query.Builder;
using MirrorM.Internal.Query.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MirrorM.Internal
{
    internal interface IContextInternal
    {
        void Add(Entity entity);
        void AddConnection(string connectionTable, string key1, Guid id1, string key2, Guid id2);
        void RemoveConnection(string connectionTable, string key1, Guid id1, string key2, Guid id2);

        IAsyncEnumerable<T> ExecuteGetQueryAsync<T>(QueryBuilder<T> builder) where T : Entity;

        Task<R> ExecuteSumQueryAsync<T, R>(QueryBuilder<T> builder) where T : Entity;
        Task<R?> ExecuteMaxQueryAsync<T, R>(QueryBuilder<T> builder) where T : Entity where R : struct;
        Task<int> ExecuteCountQueryAsync<T>(QueryBuilder<T> builder) where T : Entity;
        Task<bool> ExecuteExistsQueryAsync<T>(QueryBuilder<T> builder) where T : Entity;

        ConnectionTableStorage? GetConnectionTableStorage(string connectionTable);

        ISuperContext? GetSuperContext();
    }
}
