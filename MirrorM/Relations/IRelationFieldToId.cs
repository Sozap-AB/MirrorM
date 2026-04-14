using System.Threading.Tasks;

namespace MirrorM.Relations
{
    public interface IRelationFieldToId<T> where T : EntityBase
    {
        Task<T> GetAsync();
        Task<T?> GetOptionalAsync();
        void AttachTo(T entity);
    }
}
