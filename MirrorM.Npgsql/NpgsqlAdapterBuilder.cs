using MirrorM.AdapterInterface;
using MirrorM.Npgsql.Internal;
using System;

namespace MirrorM.DatabaseAdapters.Postgresql
{
    public class NpgsqlAdapterBuilder
    {
        internal string? ConnectionString { get; private set; }

        public NpgsqlAdapterBuilder()
        {
        }

        public NpgsqlAdapterBuilder UseConnectionString(string connectionString)
        {
            ConnectionString = connectionString;

            return this;
        }

        public IDatabaseAdapter Build()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new InvalidOperationException("Connection string is not set.");

            return new Adapter(ConnectionString);
        }
    }
}
