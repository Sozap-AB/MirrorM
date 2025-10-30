using System;
using System.Threading.Tasks;

namespace MirrorM.AdapterInterface
{
    public interface IDatabaseAdapter : IDisposable
    {
        Task<IDatabaseConnection> CreateConnectionAsync();
    }
}
