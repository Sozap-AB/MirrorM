using MirrorM.Tests.Models;

namespace MirrorM.Tests
{
    public partial class DatabaseTests
    {
        [Fact]
        public async Task TransactionChangesMergeTest()
        {
            var playerId = await ExecuteWithDatabaseAndGetAsync(db =>
            {
                return Task.FromResult(new Player(db, "player1", 5).Id);
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player = await db.GetByIdAsync<Player>(playerId);

                player.Level = 10;

                await db.ExecuteTransactionAsync(async () =>
                {
                    var txPlayer = await db.GetByIdAsync<Player>(playerId);

                    txPlayer.Name = "player2";
                });
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player = await db.GetByIdAsync<Player>(playerId);

                Assert.Equal("player2", player.Name);
                Assert.Equal(10, player.Level);
                Assert.Equal(3, player.Version);
            });
        }
    }
}