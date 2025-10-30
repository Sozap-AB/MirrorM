using MirrorM.Config;
using MirrorM.DatabaseAdapters.Postgresql;

namespace MirrorM.Npgsql
{
    public static class ContextProviderConfigBuilderExtensions
    {
        public static ContextProviderConfigBuilder UseNpgsqlAdapter(this ContextProviderConfigBuilder builder, string connectionString)
        {
            builder.UseDatabaseAdapter(
                new NpgsqlAdapterBuilder()
                    .UseConnectionString(connectionString)
                    .Build()
            );

            return builder;
        }
    }
}
