using MirrorM.Tests.Models;
using System.Text.Json.Nodes;

namespace MirrorM.Tests
{
    public partial class DatabaseTests
    {
        [Fact]
        public async Task JsonNodeConverterTest()
        {
            await ExecuteWithDatabaseAsync(db =>
            {
                var details = new PlayerDetails(db, new Player(db, "player1", 5));

                details.MetaData = new JsonObject
                {
                    ["age"] = 30,
                    ["currency"] = "SEK"
                };

                return Task.CompletedTask;
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player = await db.Query<Player>().FirstAsync(x => x.Name == "player1");
                var details = await player.PlayerDetails.GetAsync();

                Assert.Equal(30, details.MetaData["age"]!.GetValue<int>());
                Assert.Equal("SEK", details.MetaData["currency"]!.GetValue<string>());
            });
        }
    }
}
