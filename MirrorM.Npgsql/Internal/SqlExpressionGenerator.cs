using MirrorM.AdapterInterface.Query;
using MirrorM.AdapterInterface.Query.Conditions;
using MirrorM.AdapterInterface.Query.Connections;
using MirrorM.Attributes;
using MirrorM.Common;
using MirrorM.Internal.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MirrorM.Npgsql.Internal
{
    internal static class SqlExpressionGenerator
    {
        private sealed class ParametersCollection
        {
            private IList<SqlParameter> ParametersWritable { get; } = new List<SqlParameter>();

            public IEnumerable<SqlParameter> Parameters => ParametersWritable;

            public string AddParameter(SqlParameterValue value)
            {
                var nextP = $"p{ParametersWritable.Count + 1}";

                ParametersWritable.Add(new SqlParameter(nextP, value));

                return nextP;
            }
        }

        public static SqlExpression GenerateEntitySelectSql(IEntityQuerySchema schema)
        {
            var parameters = new ParametersCollection();

            StringBuilder sqlBuilder = new StringBuilder();

            sqlBuilder.Append("SELECT m.* FROM ");

            AppendTableName(schema, sqlBuilder);

            AppendConditions(schema, sqlBuilder, parameters); // WHERE ...
            AppendSortings(schema, sqlBuilder); // ORDER BY ...
            AppendSkip(schema, sqlBuilder); // OFFSET ...
            AppendTake(schema, sqlBuilder); // LIMIT ...

            // we're wrapping main query in joins to make skip & limit work properly
            // TODO: it's worth comparing performance, maybe it's worth wrapping ONLY if we have take or skip

            if (schema.Joins.Any())
            {
                StringBuilder wrappingBuilder = new StringBuilder("SELECT m.*");

                schema.Joins.ForEach((j, i) =>
                {
                    wrappingBuilder.Append($", t{i + 1}.*");
                });

                wrappingBuilder.Append($" FROM ({sqlBuilder.ToString()}) m");

                schema.Joins.ForEach((join, i) =>
                {
                    var fType = join.ForeignType.GetCustomAttribute<EntityAttribute>() ?? throw new InvalidOperationException(); //TODO: throw specific exception

                    wrappingBuilder.Append($" LEFT JOIN {fType.TableName} t{i + 1} ON m.{join.OwnField} = t{i + 1}.{join.ForeignField}");
                });

                sqlBuilder = wrappingBuilder;
            }

            return new SqlExpression(
                sqlBuilder.ToString(),
                parameters.Parameters
            );
        }

        public static SqlExpression GenerateEntitySumSql(IEntityAggregateQuerySchema schema)
        {
            return GenerateEntityAggregateSql(schema, "SUM", schema.AggregateField!.Name);
        }

        public static SqlExpression GenerateEntityMaxSql(IEntityAggregateQuerySchema schema)
        {
            return GenerateEntityAggregateSql(schema, "MAX", schema.AggregateField!.Name);
        }

        public static SqlExpression GenerateEntityCountSql(IEntityAggregateQuerySchema schema)
        {
            return GenerateEntityAggregateSql(schema, "COUNT");
        }

        public static SqlExpression GenerateEntityExistsSql(IEntityAggregateQuerySchema schema)
        {
            var parameters = new ParametersCollection();

            StringBuilder sqlBuilder = new StringBuilder();

            sqlBuilder.Append($"SELECT EXISTS (SELECT 1 FROM ");

            AppendTableName(schema, sqlBuilder);

            AppendConditions(schema, sqlBuilder, parameters); // WHERE ...
            AppendSortings(schema, sqlBuilder); // ORDER BY ...
            AppendSkip(schema, sqlBuilder); // OFFSET ...

            sqlBuilder.Append(")");

            return new SqlExpression(
                sqlBuilder.ToString(),
                parameters.Parameters
            );
        }

        private static SqlExpression GenerateEntityAggregateSql(IEntityAggregateQuerySchema schema, string aggregateFunction, string field = "*")
        {
            var parameters = new ParametersCollection();

            StringBuilder sqlBuilder = new StringBuilder();

            sqlBuilder.Append($"SELECT {aggregateFunction}(m.{field}) FROM ");

            AppendTableName(schema, sqlBuilder);

            AppendConditions(schema, sqlBuilder, parameters); // WHERE ...
            AppendSortings(schema, sqlBuilder); // ORDER BY ...
            AppendSkip(schema, sqlBuilder); // OFFSET ...
            AppendTake(schema, sqlBuilder); // LIMIT ...

            return new SqlExpression(
                sqlBuilder.ToString(),
                parameters.Parameters
            );
        }

        public static SqlExpression GenerateEntityInsertSql(IEntityInsertSchema schema)
        {
            var parameters = new ParametersCollection();

            StringBuilder sqlBuilder = new StringBuilder();

            sqlBuilder.Append($"INSERT INTO {schema.EntityType.GetCustomAttribute<EntityAttribute>()!.TableName} (");
            sqlBuilder.Append(string.Join(", ", schema.Fields.Select(f => f.Name)));
            sqlBuilder.Append(") VALUES (");
            sqlBuilder.Append(string.Join(", ", schema.Fields.Select(f => WrapWritableItem(f.Value, parameters))));
            sqlBuilder.Append(")");

            return new SqlExpression(
                sqlBuilder.ToString(),
                parameters.Parameters
            );
        }

        public static SqlExpression GenerateEntityUpdateSql(IEntityUpdateSchema schema)
        {
            var parameters = new ParametersCollection();
            StringBuilder sqlBuilder = new StringBuilder();

            sqlBuilder.Append($"UPDATE {schema.EntityType.GetCustomAttribute<EntityAttribute>()!.TableName} m SET ");
            sqlBuilder.Append(string.Join(", ", schema.Fields.Select(f => $"{f.Name} = {WrapWritableItem(f.Value, parameters)}")));
            sqlBuilder.Append(" WHERE ");
            sqlBuilder.Append(string.Join(" AND ", schema.Conditions.Select(c => GenerateConditionSql(c, parameters))));

            return new SqlExpression(
                sqlBuilder.ToString(),
                parameters.Parameters
            );
        }

        public static SqlExpression GenerateEntityDeleteSql(IEntityDeleteSchema schema)
        {
            var parameters = new ParametersCollection();
            StringBuilder sqlBuilder = new StringBuilder();

            sqlBuilder.Append($"DELETE FROM {schema.EntityType.GetCustomAttribute<EntityAttribute>()!.TableName} m WHERE ");
            sqlBuilder.Append(string.Join(" AND ", schema.Conditions.Select(c => GenerateConditionSql(c, parameters))));

            return new SqlExpression(
                sqlBuilder.ToString(),
                parameters.Parameters
            );
        }

        public static SqlExpression GenerateConnectionInsertSql(IConnectionInsertSchema schema)
        {
            var parameters = new ParametersCollection();
            StringBuilder sqlBuilder = new StringBuilder();

            sqlBuilder.Append($"INSERT INTO {schema.Table} ({schema.Key1}, {schema.Key2}) VALUES ({WrapConstant(schema.Value1, parameters)}, {WrapConstant(schema.Value2, parameters)})");

            return new SqlExpression(
                sqlBuilder.ToString(),
                parameters.Parameters
            );
        }

        public static SqlExpression GenerateConnectionDeleteSql(IConnectionDeleteSchema schema)
        {
            var parameters = new ParametersCollection();
            StringBuilder sqlBuilder = new StringBuilder();

            sqlBuilder.Append($"DELETE FROM {schema.Table} WHERE {schema.Key1} = {WrapConstant(schema.Value1, parameters)} AND {schema.Key2} = {WrapConstant(schema.Value2, parameters)}");

            return new SqlExpression(
                sqlBuilder.ToString(),
                parameters.Parameters
            );
        }

        private static string GenerateConditionSql(ExpressionBase exp, ParametersCollection parametersCollection)
        {
            string ConvertBinaryOperation(ExpressionBinary.Operation op)
            {
                return op switch
                {
                    ExpressionBinary.Operation.Equal => "=",
                    ExpressionBinary.Operation.NotEqual => "<>",
                    ExpressionBinary.Operation.GreaterThan => ">",
                    ExpressionBinary.Operation.GreaterThanOrEqual => ">=",
                    ExpressionBinary.Operation.LessThan => "<",
                    ExpressionBinary.Operation.LessThanOrEqual => "<=",
                    ExpressionBinary.Operation.Add => "+",
                    ExpressionBinary.Operation.Subtract => "-",
                    ExpressionBinary.Operation.Multiply => "*",
                    ExpressionBinary.Operation.Divide => "/",
                    ExpressionBinary.Operation.And => "AND",
                    ExpressionBinary.Operation.Or => "OR",
                    _ => throw new NotSupportedException($"Unsupported binary operator: {exp}"),
                };
            }

            string ConvertBinaryNullComparison(ExpressionBinary.Operation op)
            {
                return op switch
                {
                    ExpressionBinary.Operation.Equal => "IS",
                    ExpressionBinary.Operation.NotEqual => "IS NOT",
                    _ => throw new NotSupportedException($"Unsupported binary null comparison operator: {exp}"),
                };
            }

            switch (exp)
            {
                case ExpressionBinary expressionBinary:
                    if (expressionBinary.Right is ExpressionConst ec && ec.Value == null)
                    {
                        return $"({GenerateConditionSql(expressionBinary.Left, parametersCollection)} {ConvertBinaryNullComparison(expressionBinary.Op)} NULL)";
                    }
                    else
                    {
                        return $"({GenerateConditionSql(expressionBinary.Left, parametersCollection)} {ConvertBinaryOperation(expressionBinary.Op)} {GenerateConditionSql(expressionBinary.Right, parametersCollection)})";
                    }
                case ExpressionNot expressionNot:
                    return $"NOT ({GenerateConditionSql(expressionNot.Operand, parametersCollection)})";
                case ExpressionConst expressionConst:
                    return WrapConstant(expressionConst.Value, parametersCollection);
                case ExpressionField expressionField:
                    return $"m.{expressionField.FieldName}";
                case ExpressionConnectionMatch expressionConnectionMatch:
                    return $"EXISTS (SELECT 1 FROM {expressionConnectionMatch.ConnectionTable} c WHERE " +
                        $"c.{expressionConnectionMatch.OwnerKey} = m.id AND " +
                        $"c.{expressionConnectionMatch.ForeignKey} = :{parametersCollection.AddParameter(new SqlParameterValue(expressionConnectionMatch.Value))}" +
                    ")";
                case ExpressionRawSql expressionRawSql:
                    var sql = expressionRawSql.Sql;

                    expressionRawSql.Parameters.ForEach(p =>
                    {
                        sql = sql.Replace($":{p.Name}", $":{parametersCollection.AddParameter(p.Value)}");

                        parametersCollection.AddParameter(p.Value);
                    });

                    return $"({sql})";
                default:
                    throw new NotSupportedException($"Unsupported expression type: {exp}");
            }
        }

        private static string WrapWritableItem(SqlParameterValue v, ParametersCollection parametersCollection)
        {
            if (v.Value == DBNull.Value)
            {
                return "NULL";
            }

            return $":{parametersCollection.AddParameter(v)}";
        }

        private static string WrapConstant(object? v, ParametersCollection parametersCollection)
        {
            bool IsWrappedNull(object boxed)
            {
                return !(bool)boxed.GetType().GetProperty("HasValue")!.GetValue(boxed)!;
            }

            if (v == null)
            {
                return "NULL";
            }
            else
            {
                var unerlyingNullableType = Nullable.GetUnderlyingType(v.GetType());

                if (unerlyingNullableType != null && IsWrappedNull(v))
                {
                    return "NULL";
                }
            }

            return $":{parametersCollection.AddParameter(new SqlParameterValue(v))}";
        }

        private static void AppendConditions(IBaseEntityQuerySchema schema, StringBuilder sqlBuilder, ParametersCollection parameters)
        {
            if (schema.Conditions.Any())
            {
                sqlBuilder.Append(" WHERE ");

                schema.Conditions.ForEach((condition, idx) =>
                {
                    if (idx != 0)
                        sqlBuilder.Append(" AND ");

                    sqlBuilder.Append(GenerateConditionSql(condition, parameters));
                });
            }
        }

        private static void AppendSortings(IBaseEntityQuerySchema schema, StringBuilder sqlBuilder)
        {
            if (schema.Sortings.Any())
            {
                sqlBuilder.Append(" ORDER BY ");

                schema.Sortings.ForEach((sorting, idx) =>
                {
                    if (idx != 0)
                        sqlBuilder.Append(", ");

                    sqlBuilder.Append($"m.{sorting.PropertyName} {(sorting.Ascending ? "ASC" : "DESC")}");
                });
            }
        }

        private static void AppendSkip(IBaseEntityQuerySchema schema, StringBuilder sqlBuilder)
        {
            if (schema.SkipCount > 0)
            {
                sqlBuilder.Append($" OFFSET {schema.SkipCount}");
            }
        }

        private static void AppendTake(IBaseEntityQuerySchema schema, StringBuilder sqlBuilder)
        {
            if (schema.TakeCount.HasValue)
            {
                sqlBuilder.Append($" LIMIT {schema.TakeCount.Value}");
            }
        }

        private static void AppendTableName(IBaseEntityQuerySchema schema, StringBuilder sqlBuilder)
        {
            var mainEntity = schema.EntityType.GetCustomAttribute<EntityAttribute>() ?? throw new InvalidOperationException(); //TODO: throw specific exception

            sqlBuilder.Append($"{mainEntity.TableName} m");
        }
    }
}