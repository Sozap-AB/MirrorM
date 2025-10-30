using MirrorM;
using MirrorM.Config;
using MirrorM.Npgsql;
using MirrorM.Tests.Tools;

namespace FishingTourServer.Controllers.Client
{
    public class TestBase
    {
        private IContextProvider ContextProvider { get; }

        protected TestBase()
        {
            ContextProvider = new ContextProviderConfigBuilder()
                .UseNpgsqlAdapter(Constants.CONNECTION_STRING)
                .Build();
        }

        protected async Task ExecuteWithDatabaseAsync(Func<IContext, Task> func)
        {
            using var context = await ContextProvider.CreateContextAsync();

            await func(context);

            await context.CommitAsync();
        }

        protected async Task<T> ExecuteWithDatabaseAndGetAsync<T>(Func<IContext, Task<T>> func)
        {
            T? result = default;

            await ExecuteWithDatabaseAsync(async ctx =>
            {
                result = await func(ctx);
            });

            return result!;
        }
    }
}