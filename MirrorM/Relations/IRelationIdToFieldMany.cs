namespace MirrorM.Relations
{
    public interface IRelationIdToFieldMany<T> where T : Entity
    {
        IQuery<T> Query();

        void Attach(T entity);
        void Detach(T entity);
    }
}
