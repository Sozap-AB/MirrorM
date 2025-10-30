using System.Reflection;

namespace MirrorM.Internal.Reflection
{
    internal interface IReflectionCache
    {
        void PrecacheAssemblyTypes(Assembly assembly);
    }
}
