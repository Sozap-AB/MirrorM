using FishingTourServer.Controllers.Client;
using MirrorM.Tests.Fixtures;

namespace MirrorM.Tests
{
    [Collection(DatabaseCollection.NAME)]
    public partial class DatabaseTests : TestBase, IAsyncLifetime
    {
        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await ExecuteWithDatabaseAsync(async ctx =>
            {
                foreach (var tableName in new string[] {
                    "players",
                    "player_details",
                    "player_groups",
                    "player_group_player",
                    "player_items",
                    "player_powerups"
                })
                {
                    await ctx.ExecuteSqlCommandAsync($"TRUNCATE TABLE {tableName};");
                }
            });
        }
    }
}
