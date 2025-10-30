using MirrorM.AdapterInterface;
using MirrorM.Internal.Reflection;
using System.Reflection;
using System.Threading.Tasks;

namespace MirrorM.Internal
{
    internal class ContextProvider : IContextProvider
    {
        private IReflectionCache ReflectionCache { get; }
        private IDatabaseAdapter DatabaseAdapter { get; }

        public ContextProvider(IDatabaseAdapter databaseAdapter)
        {
            DatabaseAdapter = databaseAdapter;

            ReflectionCache = new ReflectionCache();

            ReflectionCache.PrecacheAssemblyTypes(Assembly.GetEntryAssembly()); //TODO: make it configurable
        }

        public async Task<IContext> CreateContextAsync()
        {
            return new Context(ReflectionCache, await DatabaseAdapter.CreateConnectionAsync());
        }
    }
}
