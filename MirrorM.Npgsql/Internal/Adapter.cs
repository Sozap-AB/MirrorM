using MirrorM.AdapterInterface;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace MirrorM.Npgsql.Internal
{
    internal class Adapter : IDatabaseAdapter
    {
        public string ConnectionString { get; }

        private NpgsqlDataSource Source { get; }

        public Adapter(string connectionString)
        {
            ConnectionString = connectionString;

            Source = BuildSource();
        }

        private NpgsqlDataSource BuildSource()
        {
            return new NpgsqlDataSourceBuilder(ConnectionString).Build();
        }

        public async Task<IDatabaseConnection> CreateConnectionAsync()
        {
            return new Connection(await Source.OpenConnectionAsync());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Source.Dispose();
        }
    }
}
