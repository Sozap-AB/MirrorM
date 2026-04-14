using MirrorM.Config;
using MirrorM.Npgsql;
using MirrorM.Tests.Converters;
using MirrorM.Tests.Models;
using Npgsql.NameTranslation;

namespace MirrorM.Tests.Tools
{
    public class TestBase
    {
        protected IContextProvider ContextProvider { get; }

        protected TestBase()
        {
            ContextProvider = new ContextProviderConfigBuilder()
                .UseNpgsqlAdapter(Constants.CONNECTION_STRING, dsb =>
                {
                    dsb.MapEnum<Player.Status>("player_status", new NpgsqlNullNameTranslator());
                })
                .UseTypeConverters(new JsonNodeConverter())
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