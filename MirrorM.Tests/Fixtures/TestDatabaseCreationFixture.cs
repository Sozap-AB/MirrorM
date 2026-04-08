using MirrorM.Tests.Tools;
using Npgsql;

namespace MirrorM.Tests.Fixtures
{
    internal class TestDatabaseCreationFixture : IAsyncLifetime
    {
        public async Task InitializeAsync()
        {
            try
            {
                using (var conn = await new NpgsqlDataSourceBuilder(Constants.DATABASE_INDEPENDENT_CONNECTION_STRING).Build().OpenConnectionAsync())
                {
                    await using (var cmd = new NpgsqlCommand($"""
                        CREATE DATABASE {Constants.DATABASE_NAME}
                        WITH OWNER = postgres
                        ENCODING = 'UTF8'
                        CONNECTION LIMIT = -1
                    """, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (PostgresException e)
            {
                if (e.SqlState == PostgresErrorCodes.DuplicateDatabase)
                {
                    await DropDatabaseAsync();

                    await InitializeAsync();

                    return;
                }
            }

            using (var conn = await new NpgsqlDataSourceBuilder(Constants.CONNECTION_STRING).Build().OpenConnectionAsync())
            {
                using Stream stream = typeof(TestDatabaseCreationFixture).Assembly.GetManifestResourceStream("MirrorM.Tests.Resources.TestPgDatabaseInit.sql")!;
                using StreamReader reader = new StreamReader(stream);

                await using var cmd = new NpgsqlCommand(await reader.ReadToEndAsync(), conn);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task DisposeAsync()
        {
            await DropDatabaseAsync();
        }

        private static async Task DropDatabaseAsync()
        {
            using (var conn = await new NpgsqlDataSourceBuilder(Constants.DATABASE_INDEPENDENT_CONNECTION_STRING).Build().OpenConnectionAsync())
            {
                await using (var cmd = new NpgsqlCommand($"DROP DATABASE {Constants.DATABASE_NAME}", conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}