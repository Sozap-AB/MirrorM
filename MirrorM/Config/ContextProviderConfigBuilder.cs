using MirrorM.AdapterInterface;
using MirrorM.Internal;
using MirrorM.TypeConversion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MirrorM.Config
{
    public class ContextProviderConfigBuilder
    {
        internal IDatabaseAdapter? DatabaseAdapter { get; private set; }
        internal IList<ITypeConverter>? TypeConverters { get; private set; }

        public ContextProviderConfigBuilder UseDatabaseAdapter(IDatabaseAdapter adapter)
        {
            DatabaseAdapter = adapter;

            return this;
        }

        public ContextProviderConfigBuilder UseTypeConverters(IEnumerable<ITypeConverter> converters)
        {
            TypeConverters = converters.ToList();

            return this;
        }

        public ContextProviderConfigBuilder UseTypeConverters(params ITypeConverter[] converters)
        {
            return UseTypeConverters((IEnumerable<ITypeConverter>)converters);
        }

        public ContextProviderConfigBuilder AddTypeConverter(ITypeConverter converter)
        {
            if (TypeConverters == null)
                TypeConverters = new List<ITypeConverter>();

            TypeConverters.Add(converter);

            return this;
        }

        public IContextProvider Build()
        {
            if (DatabaseAdapter == null)
                throw new InvalidOperationException("Database adapter is not set.");

            return new ContextProvider(
                DatabaseAdapter,
                TypeConverters ?? Array.Empty<ITypeConverter>()
            );
        }
    }
}
