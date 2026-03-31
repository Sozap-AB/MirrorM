using MirrorM.AdapterInterface;
using MirrorM.TypeConversion;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MirrorM.Internal
{
    internal class ContextProvider : IContextProvider
    {
        private IDatabaseAdapter DatabaseAdapter { get; }
        private IEnumerable<ITypeConverter> TypeConverters { get; }

        public ContextProvider(IDatabaseAdapter databaseAdapter, IEnumerable<ITypeConverter> typeConverters)
        {
            DatabaseAdapter = databaseAdapter;
            TypeConverters = typeConverters;
        }

        public async Task<IContext> CreateContextAsync()
        {
            return new Context(await DatabaseAdapter.CreateConnectionAsync(), TypeConverters);
        }
    }
}
