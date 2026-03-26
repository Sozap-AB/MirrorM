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
        private Action<NpgsqlDataSourceBuilder>? NpgsqlConfigurer { get; }

        public Adapter(string connectionString, Action<NpgsqlDataSourceBuilder>? npgsqlConfigurer)
        {
            ConnectionString = connectionString;
            NpgsqlConfigurer = npgsqlConfigurer;

            Source = BuildSource();
        }

        private NpgsqlDataSource BuildSource()
        {
            var builder = new NpgsqlDataSourceBuilder(ConnectionString);

            if (NpgsqlConfigurer != null)
                NpgsqlConfigurer(builder);

            return builder.Build();
        }

        public async Task<IDatabaseConnection> CreateConnectionAsync()
        {
            return new Connection(await Source.OpenConnectionAsync());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // csharpsquid:S3881 recommendation
        }

        protected virtual void Dispose(bool disposing)
        {
            Source.Dispose();
        }
    }
}
