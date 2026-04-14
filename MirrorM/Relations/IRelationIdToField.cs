using System.Threading.Tasks;

namespace MirrorM.Relations
{
    public interface IRelationIdToField<T> where T : EntityBase
    {
        Task<T> GetAsync();
        Task<T?> GetOptionalAsync();
        void Attach(T entity);
    }
}
