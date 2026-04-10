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
    }
}
