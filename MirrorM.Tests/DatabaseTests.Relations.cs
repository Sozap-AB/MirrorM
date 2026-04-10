using MirrorM.Tests.Models;

namespace MirrorM.Tests
{
    public partial class DatabaseTests
    {
        [Fact]
        public async Task OneToOneSimpleRelationTest()
        {
            (var player1Id, var player2Id) = await ExecuteWithDatabaseAndGetAsync(db =>
            {
                return Task.FromResult((
                    new Player(db, "player1", 5).Id,
                    new Player(db, "player2", 5).Id
                ));
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player = await db.GetByIdAsync<Player>(player1Id);
                Assert.Null(await player.PlayerDetails.GetOptionalAsync());
            });

            (var playerDetail1Id, var playerDetail2Id) = await ExecuteWithDatabaseAndGetAsync(async db =>
            {
                var player1 = await db.GetByIdAsync<Player>(player1Id);
                var details1 = new PlayerDetails(db);

                var player2 = await db.GetByIdAsync<Player>(player2Id);
                var details2 = new PlayerDetails(db);

                player1.PlayerDetails.AttachTo(details1);
                details2.Player.Attach(player2);

                return (details1.Id, details2.Id);
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player1 = await db.GetByIdAsync<Player>(player1Id);
                var details1 = await player1.PlayerDetails.GetAsync();

                Assert.Equal(player1Id, details1.PlayerId);
                Assert.Equal(playerDetail1Id, details1.Id);

                var player2 = await db.GetByIdAsync<Player>(player2Id);
                var details2 = await player2.PlayerDetails.GetAsync();

                Assert.Equal(player2Id, details2.PlayerId);
                Assert.Equal(playerDetail2Id, details2.Id);
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
                Assert.False(await player.PlayerItems.Query().AnyAsync());
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player = await db.GetByIdAsync<Player>(playerId);
                var inventoryItem1 = new PlayerInventoryItem(db, "item1");
                var inventoryItem2 = new PlayerInventoryItem(db, "item2");

                inventoryItem1.Player.Attach(player);
                player.PlayerItems.AttachTo(inventoryItem2);
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player = await db.GetByIdAsync<Player>(playerId);

                var items = await player.PlayerItems.Query().ToListAsync();

                Assert.Equal(2, items.Count());
                Assert.Equal(["item1", "item2"], items.Select(i => i.Name).OrderBy(x => x));

                player.PlayerItems.DetachFrom(items.First(x => x.Name == "item1"));
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player = await db.GetByIdAsync<Player>(playerId);

                var items = await player.PlayerItems.Query().ToListAsync();

                Assert.Single(items);
                Assert.Equal(["item2"], items.Select(i => i.Name).OrderBy(x => x));
            });
        }

        [Fact]
        public async Task ManyToManySimpleRelationTest()
        {
            Guid user1Id = Guid.Empty;
            Guid user2Id = Guid.Empty;
            Guid group1Id = Guid.Empty;
            Guid group2Id = Guid.Empty;

            await ExecuteWithDatabaseAsync(db =>
            {
                db.SetSqlInterceptor((sql, parameters) =>
                {
                    Console.WriteLine(sql);
                });

                var group1 = new PlayerGroup(db, "group1");
                var group2 = new PlayerGroup(db, "group2");

                var player1 = new Player(db, "player1", 1);
                var player2 = new Player(db, "player2", 1);

                user1Id = player1.Id;
                user2Id = player2.Id;

                group1Id = group1.Id;
                group2Id = group2.Id;

                group1.Players.AttachTo(player1);
                player2.Groups.AttachTo(group1);
                group2.Players.AttachTo(player1);

                return Task.CompletedTask;
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player1 = await db.GetByIdAsync<Player>(user1Id);
                var player2 = await db.GetByIdAsync<Player>(user2Id);
                var group1 = await db.GetByIdAsync<PlayerGroup>(group1Id);
                var group2 = await db.GetByIdAsync<PlayerGroup>(group2Id);

                Assert.True(new HashSet<Player>([player1, player2])
                    .SetEquals(await group1.Players.Query().ToListAsync()));

                Assert.True(new HashSet<Player>([player1])
                    .SetEquals(await group2.Players.Query().ToListAsync()));

                Assert.True(new HashSet<PlayerGroup>([group1, group2])
                    .SetEquals(await player1.Groups.Query().ToListAsync()));

                Assert.True(new HashSet<PlayerGroup>([group1])
                    .SetEquals(await player2.Groups.Query().ToListAsync()));

                player1.Groups.DetachFrom(group2);
            });

            await ExecuteWithDatabaseAsync(async db =>
            {
                var player1 = await db.GetByIdAsync<Player>(user1Id);
                var player2 = await db.GetByIdAsync<Player>(user2Id);
                var group1 = await db.GetByIdAsync<PlayerGroup>(group1Id);
                var group2 = await db.GetByIdAsync<PlayerGroup>(group2Id);

                Assert.True(new HashSet<Player>([player1, player2])
                    .SetEquals(await group1.Players.Query().ToListAsync()));

                Assert.True(new HashSet<Player>([])
                    .SetEquals(await group2.Players.Query().ToListAsync()));

                Assert.True(new HashSet<PlayerGroup>([group1])
                    .SetEquals(await player1.Groups.Query().ToListAsync()));

                Assert.True(new HashSet<PlayerGroup>([group1])
                    .SetEquals(await player2.Groups.Query().ToListAsync()));
            });
        }
    }
}
