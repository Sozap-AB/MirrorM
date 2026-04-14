namespace MirrorM.Relations
{
    public interface IRelationIdManyToIdMany<T> where T : EntityBase
    {
        IQuery<T> Query();

        void Attach(T entity);
        void Detach(T entity);
    }
}
