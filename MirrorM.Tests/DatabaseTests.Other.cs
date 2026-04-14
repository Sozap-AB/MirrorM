using MirrorM.Tests.Models;
using MirrorM.Tests.Tools;

namespace MirrorM.Tests
{
    public partial class DatabaseTests
    {
        [Fact]
        public async Task SuperContextTest()
        {
            await ExecuteWithDatabaseAsync(db =>
            {
                db.SetSuperContext(new RankCalculator());

                var player = new Player(db, "player1", 5);

                Assert.Equal("Beginner", player.Rank);

                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task SqlInterceptorTest()
        {
            string? insertSql = null;

            await ExecuteWithDatabaseAsync(db =>
            {
                db.SetSqlInterceptor((sql, parameters) =>
                {
                    if (sql.StartsWith("INSERT"))
                        insertSql = sql;
                });

                _ = new Player(db, "player1", 5);

                return Task.CompletedTask;
            });

            Assert.Equal("INSERT INTO players (id, _version, _created_at, _updated_at, name, level, status) VALUES (:p1, :p2, :p3, :p4, :p5, :p6, :p7)", insertSql);
        }

        [Fact]
        public async Task DelayedInitializationTest()
        {
            var playerId = await ExecuteWithDatabaseAndGetAsync(async db =>
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await db.InitializeAsync();
                });

                return new Player(db, "player1", 5).Id;
            });

            using (var context = ContextProvider.CreateUninitializedContext())
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await context.Query<Player>().FirstAsync(x => x.Name == "player1");
                });

                await context.InitializeAsync();

                Assert.Equal(
                    playerId,
                    (await context.Query<Player>().FirstAsync(x => x.Name == "player1")).Id
                );

                await context.CommitAsync();
            }
        }
    }
}
