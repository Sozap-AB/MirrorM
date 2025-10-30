using MirrorM.AdapterInterface.Query;
using MirrorM.AdapterInterface.Query.Conditions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MirrorM.Internal.Query.Builder
{
    internal class QueryBuilder<T> : IQuery<T>, IEntityQuerySchema, IEntityAggregateQuerySchema where T : Entity
    {
        private List<ExpressionBase> ConditionsWritable { get; } = new List<ExpressionBase>();
        private List<Sorting> SortingsWritable { get; } = new List<Sorting>();
        private List<Join> JoinsWritable { get; } = new List<Join>();

        public int SkipCount { get; private set; } = 0;
        public int? TakeCount { get; private set; } = null;
        public Type EntityType => typeof(T);

        public Field? AggregateField { get; private set; } = null;

        public IEnumerable<ExpressionBase> Conditions => ConditionsWritable;
        public IEnumerable<Sorting> Sortings => SortingsWritable;
        public IEnumerable<Join> Joins => JoinsWritable;

        private IContextInternal Context { get; }

        public QueryBuilder(IContextInternal context)
        {
            Context = context;
        }

        public IQuery<T> Where(Expression<Func<T, bool>> predicate)
        {
            //TODO: if we have AND condition, we can split it to multiple conditions

            ConditionsWritable.Add(ConditionsGenerator.GenerateFromPredicate(predicate));

            return this;
        }

        internal IQuery<T> WhereConnectionForeignMatch(string connectionTable, string ownKey, string foreignKey, Guid val)
        {
            ConditionsWritable.Add(new ExpressionConnectionMatch(connectionTable, ownKey, foreignKey, val));

            return this;
        }

        public IQuery<T> WhereRawSQL(string sql)
        {
            throw new NotImplementedException();
        }

        public IQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            //Sortings.Add(); //TODO: get field name from keySelector

            return this;
        }

        public IQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            throw new NotImplementedException();
        }

        public IQuery<T> Skip(int count)
        {
            SkipCount = count;

            return this;
        }

        public IQuery<T> Take(int count)
        {
            TakeCount = count;

            return this;
        }

        public Task<bool> AnyAsync()
        {
            return Context.ExecuteExistsQueryAsync(this);
        }

        public Task<R?> MaxAsync<R>(Expression<Func<T, R>> fieldSelector) where R : struct
        {
            AggregateField = FieldGenerator.GenerateFromExpression(fieldSelector);

            return Context.ExecuteMaxQueryAsync<T, R>(this);
        }

        public Task<R> SumAsync<R>(Expression<Func<T, R>> fieldSelector)
        {
            AggregateField = FieldGenerator.GenerateFromExpression(fieldSelector);

            return Context.ExecuteSumQueryAsync<T, R>(this);
        }

        public Task<int> CountAsync()
        {
            return Context.ExecuteCountQueryAsync(this);
        }

        public IAsyncEnumerable<T> ToAsyncEnumerable()
        {
            return Context.ExecuteGetQueryAsync(this);
        }

        public Task DeleteAsync()
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Expression<Func<T>> updater)
        {
            throw new NotImplementedException();
        }
    }
}
