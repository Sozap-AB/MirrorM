using System.Threading.Tasks;

namespace MirrorM.Relations
{
    public interface IRelationFieldToId<T> where T : Entity
    {
        Task<T> GetAsync();
        Task<T?> GetOptionalAsync();
        void AttachTo(T entity);
    }
}
