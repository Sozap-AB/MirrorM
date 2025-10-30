namespace MirrorM.AdapterInterface.Query
{
    public interface IEntityAggregateQuerySchema : IBaseEntityQuerySchema
    {
        Field? AggregateField { get; }
    }
}
