using MirrorM.Tests.Models;

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

        [Fact]
        public async Task OrderBySkipAndTakeTest()
        {
            await ExecuteWithDatabaseAsync(db =>
            {
                _ = new Player(db, "player1", 1);
                _ = new Player(db, "player2", 2);
                _ = new Player(db, "player3", 3);
                _ = new Player(db, "player4", 4);
                _ = new Player(db, "player5", 5);

                return Task.CompletedTask;
            });

            await ExecuteWithDatabaseAsync(async db => Assert.Equal("player1", (await db.Query<Player>().OrderBy(x => x.Level).FirstAsync()).Name));
            await ExecuteWithDatabaseAsync(async db => Assert.Equal("player5", (await db.Query<Player>().OrderByDescending(x => x.Level).FirstAsync()).Name));
            await ExecuteWithDatabaseAsync(async db =>
            {
                var result = await db.Query<Player>().OrderByDescending(x => x.Level).Skip(2).Take(2).ToArrayAsync();

                Assert.Equal(2, result.Length);
                Assert.Equal("player3", result.First().Name);
                Assert.Equal("player2", result.Last().Name);
            });
        }
    }
}
