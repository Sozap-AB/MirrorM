using MirrorM.AdapterInterface;
using MirrorM.Internal;
using System;

namespace MirrorM.Config
{
    public class ContextProviderConfigBuilder
    {
        internal IDatabaseAdapter? DatabaseAdapter { get; private set; }

        public ContextProviderConfigBuilder UseDatabaseAdapter(IDatabaseAdapter adapter)
        {
            DatabaseAdapter = adapter;

            return this;
        }

        public IContextProvider Build()
        {
            if (DatabaseAdapter == null)
                throw new InvalidOperationException("Database adapter is not set.");

            return new ContextProvider(DatabaseAdapter);
        }
    }
}
