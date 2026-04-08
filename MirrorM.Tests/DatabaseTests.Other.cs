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

            Assert.Equal("INSERT INTO players (id, _version, _created_at, _updated_at, name, level) VALUES (:p1, :p2, :p3, :p4, :p5, :p6)", insertSql);
        }
    }
}
