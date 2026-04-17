using MirrorM.Tests.Models;

namespace MirrorM.Tests
{
    public partial class DatabaseTests
    {
        [Fact]
        public async Task ArithmeticConditionsCacheTest()
        {
            var playerId = await ExecuteWithDatabaseAndGetAsync(db =>
            {
                return Task.FromResult(new Player(db, "player1", 5).Id);
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level == 5).ToListAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level > 4).ToListAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level < 6).ToListAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level >= 5).ToListAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level <= 5).ToListAsync()).First().Id);
                Assert.Null((await db.Query<Player>().Where(x => x.Level != 5).ToListAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level > 5).ToListAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level < 5).ToListAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level <= 4).ToListAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level >= 6).ToListAsync()).FirstOrDefault());

                db.SetSqlInterceptor((sql, parameters) => { Assert.Fail("SQL executed, cache not used!"); });

                // repeated queries

                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level == 5).ToListAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level > 4).ToListAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level < 6).ToListAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level >= 5).ToListAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level <= 5).ToListAsync()).First().Id);
                Assert.Null((await db.Query<Player>().Where(x => x.Level != 5).ToListAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level > 5).ToListAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level < 5).ToListAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level <= 4).ToListAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level >= 6).ToListAsync()).FirstOrDefault());

                // covered queries

                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level == 5).Where(x => x.Name == "player1").ToListAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level > 4).Where(x => x.Name == "player1").ToListAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level < 6).Where(x => x.Name == "player1").ToListAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level >= 5).Where(x => x.Name == "player1").ToListAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level <= 5).Where(x => x.Name == "player1").ToListAsync()).First().Id);
                Assert.Null((await db.Query<Player>().Where(x => x.Level != 5).Where(x => x.Name == "player1").ToListAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level > 5).Where(x => x.Name == "player1").ToListAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level < 5).Where(x => x.Name == "player1").ToListAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level <= 4).Where(x => x.Name == "player1").ToListAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level >= 6).Where(x => x.Name == "player1").ToListAsync()).FirstOrDefault());

                db.SetSqlInterceptor(null);
            });
        }

        [Fact]
        public async Task SameFieldDifferentTypesCacheTest()
        {
            await ExecuteWithDatabaseAsync(db =>
            {
                _ = new Player(db, "name1", 5);
                _ = new PlayerGroup(db, "name1", 10);

                return Task.CompletedTask;
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                Assert.Equal(5, (await db.Query<Player>().Where(x => x.Name == "name1").ToListAsync()).First().Level);
                Assert.Equal(10, (await db.Query<PlayerGroup>().Where(x => x.Name == "name1").ToListAsync()).First().MinLevel);
            });
        }

        [Fact]
        public async Task TakeQueryCacheTest()
        {
            await ExecuteWithDatabaseAsync(db =>
            {
                _ = new Player(db, "player1", 3);
                _ = new Player(db, "player2", 4);
                _ = new Player(db, "player3", 5);

                return Task.CompletedTask;
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                Assert.Equal("player3", (await db.Query<Player>().OrderByDescending(x => x.Level).Take(2).FirstAsync()).Name);

                db.SetSqlInterceptor((sql, parameters) => { Assert.Fail("SQL executed, cache not used!"); });

                Assert.Equal("player3", (await db.Query<Player>().OrderByDescending(x => x.Level).Take(1).FirstAsync()).Name);
                Assert.Equal("player3", (await db.Query<Player>().OrderByDescending(x => x.Level).Take(2).FirstAsync()).Name);

                db.SetSqlInterceptor(null);
            });
        }
    }
}
