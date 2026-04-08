using MirrorM.Tests.Models;

namespace MirrorM.Tests
{
    public partial class DatabaseTests
    {
        [Fact]
        public async Task DuplicatedEntityIdExceptionTest()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await ExecuteWithDatabaseAsync(async db =>
                {
                    Guid duplicatedId = Guid.NewGuid();

                    _ = new Player(db, duplicatedId, "player1", 5);
                    _ = new Player(db, duplicatedId, "player2", 6);
                });
            });
        }
    }
}
