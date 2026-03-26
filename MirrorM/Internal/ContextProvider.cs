using MirrorM.AdapterInterface;
using System.Threading.Tasks;

namespace MirrorM.Internal
{
    internal class ContextProvider : IContextProvider
    {
        private IDatabaseAdapter DatabaseAdapter { get; }

        public ContextProvider(IDatabaseAdapter databaseAdapter)
        {
            DatabaseAdapter = databaseAdapter;
        }

        public async Task<IContext> CreateContextAsync()
        {
            return new Context(await DatabaseAdapter.CreateConnectionAsync());
        }
    }
}
