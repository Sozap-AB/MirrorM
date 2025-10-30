using System.Threading.Tasks;

namespace MirrorM
{
    public interface IContextProvider
    {
        Task<IContext> CreateContextAsync();
    }
}
