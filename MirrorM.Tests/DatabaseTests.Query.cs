using MirrorM.Common;
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
        public async Task ContainsConditionTest()
        {
            var playerId = await ExecuteWithDatabaseAndGetAsync(db =>
            {
                return Task.FromResult(new Player(db, "player1", 5).Id);
            });

            await ExecuteWithDatabaseAsync(async db => Assert.Equal(playerId, (await db.Query<Player>().Where(x => new string[] { "player1", "player2", "player3" }.Contains(x.Name)).FirstAsync()).Id));
            await ExecuteWithDatabaseAsync(async db => Assert.Equal(playerId, (await db.Query<Player>().Where(x => new List<int>(new int[] { 1, 2, 5 }).Contains(x.Level)).FirstAsync()).Id));
            await ExecuteWithDatabaseAsync(async db => Assert.Null(await db.Query<Player>().Where(x => new string[] { "player2", "player3" }.Contains(x.Name)).FirstOrDefaultAsync()));
            await ExecuteWithDatabaseAsync(async db => Assert.Null(await db.Query<Player>().Where(x => new int[] { 4, 3, 2 }.Contains(x.Level)).FirstOrDefaultAsync()));
        }

        [Fact]
        public async Task EnumConditionsTest()
        {
            await ExecuteWithDatabaseAsync(db =>
            {
                _ = new Player(db, "player1", 5);
                var player2 = new Player(db, "player2", 5);

                player2.PlayerStatus = Player.Status.Inactive;

                return Task.CompletedTask;
            });

            await ExecuteWithDatabaseAsync(async db => Assert.Equal("player1", (await db.Query<Player>().FirstAsync(x => x.PlayerStatus == Player.Status.Active)).Name));
            await ExecuteWithDatabaseAsync(async db => Assert.Equal("player2", (await db.Query<Player>().FirstAsync(x => x.PlayerStatus == Player.Status.Inactive)).Name));
            await ExecuteWithDatabaseAsync(async db => Assert.Equal("player1", (await db.Query<Player>().FirstAsync(x => x.PlayerStatus != Player.Status.Inactive)).Name));
            await ExecuteWithDatabaseAsync(async db => Assert.Equal("player2", (await db.Query<Player>().FirstAsync(x => x.PlayerStatus != Player.Status.Active)).Name));
        }

        [Fact]
        public async Task RawSqlQueryTest()
        {
            var playerId = await ExecuteWithDatabaseAndGetAsync(db =>
            {
                return Task.FromResult(new Player(db, "player1", 5).Id);
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var result = (await Player.FindByNamePartialAsync(db, "aye")).First();

                Assert.Equal(playerId, result.Id);

                var details = await PlayerDetails.InstantInsertAsync(db, playerId, 42);

                Assert.Equal(42, details.MetaData.GetValue<int>());
            });
        }

        [Fact]
        public async Task WhereRawSqlQueryTest()
        {
            var player1Id = await ExecuteWithDatabaseAndGetAsync(db =>
            {
                var player1 = new Player(db, "player1", 5);
                _ = new Player(db, "player2", 6);

                return Task.FromResult(player1.Id);
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var result = await db.Query<Player>()
                    .Where(x => x.Level == 5)
                    .WhereRawSql("name = :name", new SqlParameter("name", "player1"))
                    .ToListAsync();

                Assert.Single(result);
                Assert.Equal(player1Id, result.First().Id);
            });
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
                var result = await db.Query<Player>().OrderByDescending(x => x.Level).Skip(2).Take(2).ToListAsync();

                Assert.Equal(2, result.Count());
                Assert.Equal("player3", result.First().Name);
                Assert.Equal("player2", result.Last().Name);
            });
        }

        [Fact]
        public async Task FieldEvaluationInQueryTest()
        {
            await ExecuteWithDatabaseAsync(db =>
            {
                _ = new Player(db, "player1", 11);
                _ = new PlayerGroup(db, "group1", 10);

                return Task.CompletedTask;
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player = await db.Query<Player>().FirstAsync(x => x.Name == "player1");

                // player.Level should be evaluated, while x.MinLevel should be treated as a field reference
                var group = await db.Query<PlayerGroup>().FirstAsync(x => x.MinLevel <= player.Level);

                Assert.Equal("group1", group.Name);
            });
        }
    }
}
