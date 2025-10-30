using FishingTourServerTests.Tests.DataAccessLayer.Models;

namespace MirrorM.Tests
{
    public partial class DatabaseTests
    {
        [Fact]
        public async Task ArithmeticConditionsTest()
        {
            var playerId = await ExecuteWithDatabaseAndGetAsync(db =>
            {
                return Task.FromResult(new Player(db, "player1", 5).Id);
            });

            await ExecuteWithDatabaseAsync(async db => Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level == 5).FirstAsync()).Id));
            await ExecuteWithDatabaseAsync(async db => Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level > 4).FirstAsync()).Id));
            await ExecuteWithDatabaseAsync(async db => Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level < 6).FirstAsync()).Id));
            await ExecuteWithDatabaseAsync(async db => Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level >= 5).FirstAsync()).Id));
            await ExecuteWithDatabaseAsync(async db => Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level <= 5).FirstAsync()).Id));
            await ExecuteWithDatabaseAsync(async db => Assert.Null(await db.Query<Player>().Where(x => x.Level != 5).FirstOrDefaultAsync()));
            await ExecuteWithDatabaseAsync(async db => Assert.Null(await db.Query<Player>().Where(x => x.Level > 5).FirstOrDefaultAsync()));
            await ExecuteWithDatabaseAsync(async db => Assert.Null(await db.Query<Player>().Where(x => x.Level < 5).FirstOrDefaultAsync()));
            await ExecuteWithDatabaseAsync(async db => Assert.Null(await db.Query<Player>().Where(x => x.Level <= 4).FirstOrDefaultAsync()));
            await ExecuteWithDatabaseAsync(async db => Assert.Null(await db.Query<Player>().Where(x => x.Level >= 6).FirstOrDefaultAsync()));
        }

        [Fact]
        public async Task BooleanConditionsTest()
        {
            var playerId = await ExecuteWithDatabaseAndGetAsync(db =>
            {
                return Task.FromResult(new Player(db, "player1", 5).Id);
            });

            await ExecuteWithDatabaseAsync(async db => Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Name == "player1" && x.Level == 5).FirstAsync()).Id));
            await ExecuteWithDatabaseAsync(async db => Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Name == "player1" || x.Level == 4).FirstAsync()).Id));
            await ExecuteWithDatabaseAsync(async db => Assert.Null(await db.Query<Player>().Where(x => x.Name == "player2" && x.Level == 5).FirstOrDefaultAsync()));
            await ExecuteWithDatabaseAsync(async db => Assert.Null(await db.Query<Player>().Where(x => x.Name == "player2" || x.Level == 4).FirstOrDefaultAsync()));
        }
    }
}
