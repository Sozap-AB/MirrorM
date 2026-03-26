using MirrorM.Config;
using MirrorM.DatabaseAdapters.Postgresql;
using Npgsql;
using System;

namespace MirrorM.Npgsql
{
    public static class ContextProviderConfigBuilderExtensions
    {
        public static ContextProviderConfigBuilder UseNpgsqlAdapter(this ContextProviderConfigBuilder builder, string connectionString)
        {
            return builder.UseDatabaseAdapter(
                new NpgsqlAdapterBuilder()
                    .UseConnectionString(connectionString)
                    .Build()
            );
        }

        public static ContextProviderConfigBuilder UseNpgsqlAdapter(this ContextProviderConfigBuilder builder, string connectionString, Action<NpgsqlDataSourceBuilder> npgsqlConfigurer)
        {
            return builder.UseDatabaseAdapter(
                new NpgsqlAdapterBuilder()
                    .UseConnectionString(connectionString)
                    .UseNpgsqlConfigurer(npgsqlConfigurer)
                    .Build()
            );
        }
    }
}
