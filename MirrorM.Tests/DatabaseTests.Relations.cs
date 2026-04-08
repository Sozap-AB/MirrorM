using MirrorM.Tests.Models;

namespace MirrorM.Tests
{
    public partial class DatabaseTests
    {
        [Fact]
        public async Task OneToOneSimpleRelationTest()
        {
            var playerId = await ExecuteWithDatabaseAndGetAsync(db =>
            {
                return Task.FromResult(new Player(db, "player1", 5).Id);
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player = await db.GetByIdAsync<Player>(playerId);
                Assert.Null(await player.PlayerDetails.GetOptionalAsync());
            });

            var playerDetailId = await ExecuteWithDatabaseAndGetAsync(async db =>
            {
                var player = await db.GetByIdAsync<Player>(playerId);
                return new PlayerDetails(db, player).Id;
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player = await db.GetByIdAsync<Player>(playerId);
                var details = await player.PlayerDetails.GetAsync();
                Assert.Equal(playerId, details.PlayerId);
                Assert.Equal(playerDetailId, (await player.PlayerDetails.GetAsync()).Id);
            });
        }

        [Fact]
        public async Task OneToManySimpleRelationTest()
        {
            var playerId = await ExecuteWithDatabaseAndGetAsync(db =>
            {
                return Task.FromResult(new Player(db, "player1", 5).Id);
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player = await db.GetByIdAsync<Player>(playerId);
                Assert.False(await player.Groups.Query().AnyAsync());
            });

            var playerDetailId = await ExecuteWithDatabaseAndGetAsync(async db =>
            {
                var player = await db.GetByIdAsync<Player>(playerId);
                return new PlayerDetails(db, player).Id;
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player = await db.GetByIdAsync<Player>(playerId);
                var details = await player.PlayerDetails.GetAsync();
                Assert.Equal(playerId, details.PlayerId);
                Assert.Equal(playerDetailId, (await player.PlayerDetails.GetAsync()).Id);
            });
        }
    }
}
