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
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level == 5).ToArrayAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level > 4).ToArrayAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level < 6).ToArrayAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level >= 5).ToArrayAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level <= 5).ToArrayAsync()).First().Id);
                Assert.Null((await db.Query<Player>().Where(x => x.Level != 5).ToArrayAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level > 5).ToArrayAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level < 5).ToArrayAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level <= 4).ToArrayAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level >= 6).ToArrayAsync()).FirstOrDefault());

                db.SetSqlInterceptor((sql, parameters) => { Assert.Fail("SQL executed, cache not used!"); });

                // repeated queries

                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level == 5).ToArrayAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level > 4).ToArrayAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level < 6).ToArrayAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level >= 5).ToArrayAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level <= 5).ToArrayAsync()).First().Id);
                Assert.Null((await db.Query<Player>().Where(x => x.Level != 5).ToArrayAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level > 5).ToArrayAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level < 5).ToArrayAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level <= 4).ToArrayAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level >= 6).ToArrayAsync()).FirstOrDefault());

                // covered queries

                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level == 5).Where(x => x.Name == "player1").ToArrayAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level > 4).Where(x => x.Name == "player1").ToArrayAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level < 6).Where(x => x.Name == "player1").ToArrayAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level >= 5).Where(x => x.Name == "player1").ToArrayAsync()).First().Id);
                Assert.Equal(playerId, (await db.Query<Player>().Where(x => x.Level <= 5).Where(x => x.Name == "player1").ToArrayAsync()).First().Id);
                Assert.Null((await db.Query<Player>().Where(x => x.Level != 5).Where(x => x.Name == "player1").ToArrayAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level > 5).Where(x => x.Name == "player1").ToArrayAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level < 5).Where(x => x.Name == "player1").ToArrayAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level <= 4).Where(x => x.Name == "player1").ToArrayAsync()).FirstOrDefault());
                Assert.Null((await db.Query<Player>().Where(x => x.Level >= 6).Where(x => x.Name == "player1").ToArrayAsync()).FirstOrDefault());

                db.SetSqlInterceptor(null);
            });
        }
    }
}
