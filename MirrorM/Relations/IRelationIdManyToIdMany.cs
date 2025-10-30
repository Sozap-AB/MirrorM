namespace MirrorM.Relations
{
    public interface IRelationIdManyToIdMany<T> where T : Entity
    {
        IQuery<T> Query();

        void AttachTo(T entity);
        void DetachFrom(T entity);
    }
}
