using MirrorM.AdapterInterface.Query.Conditions;
using System;

namespace MirrorM.Internal.Query
{
    internal static class ExpressionEvaluator
    {
        public static bool EvaluateCondition(EntityBase entity, ExpressionBase expression, IContextInternal contextInternal)
        {
            return Evaluate<bool>(entity, expression, contextInternal);
        }

        private static T Evaluate<T>(EntityBase entity, ExpressionBase expression, IContextInternal contextInternal)
        {
            switch (expression)
            {
                case ExpressionBinary binaryExpression:
                    return EvaluateBinaryExpression<T>(entity, binaryExpression, contextInternal);
                case ExpressionConst constExpression:
                    return (T)constExpression.Value;
                case ExpressionField fieldExpression:
                    return entity.Fields.GetValue<T>(fieldExpression.FieldName);
                case ExpressionConnectionMatch connectionMatchExpression:
                    return (T)(object)EvaluateConnectionMatchExpression(entity, connectionMatchExpression, contextInternal);
                default:
                    throw new NotSupportedException($"Expression type {expression.GetType()} is not supported.");
            }
        }

        private static bool EvaluateConnectionMatchExpression(EntityBase entity, ExpressionConnectionMatch expression, IContextInternal contextInternal)
        {
            var connectionTableStorage = contextInternal.GetConnectionTableStorage(expression.ConnectionTable);

            if (connectionTableStorage == null)
                return false;

            return connectionTableStorage.IsEntityConnected((key: expression.OwnerKey, entity.Id), (key: expression.ForeignKey, expression.Value));
        }

        private static T EvaluateBinaryExpression<T>(EntityBase entity, ExpressionBinary expression, IContextInternal contextInternal)
        {
            switch (expression.Op)
            {
                case ExpressionBinary.Operation.And:
                    return (T)(object)(Evaluate<bool>(entity, expression.Left, contextInternal) && Evaluate<bool>(entity, expression.Right, contextInternal));
                case ExpressionBinary.Operation.Or:
                    return (T)(object)(Evaluate<bool>(entity, expression.Left, contextInternal) || Evaluate<bool>(entity, expression.Right, contextInternal));
                case ExpressionBinary.Operation.Add:
                    return (T)(object)(Evaluate<dynamic>(entity, expression.Left, contextInternal) + Evaluate<dynamic>(entity, expression.Right, contextInternal));
                case ExpressionBinary.Operation.Subtract:
                    return (T)(object)(Evaluate<dynamic>(entity, expression.Left, contextInternal) - Evaluate<dynamic>(entity, expression.Right, contextInternal));
                case ExpressionBinary.Operation.Multiply:
                    return (T)(object)(Evaluate<dynamic>(entity, expression.Left, contextInternal) * Evaluate<dynamic>(entity, expression.Right, contextInternal));
                case ExpressionBinary.Operation.Divide:
                    return (T)(object)(Evaluate<dynamic>(entity, expression.Left, contextInternal) / Evaluate<dynamic>(entity, expression.Right, contextInternal));
                case ExpressionBinary.Operation.Equal:
                    return (T)(object)Evaluate<dynamic>(entity, expression.Left, contextInternal).Equals(Evaluate<dynamic>(entity, expression.Right, contextInternal));
                case ExpressionBinary.Operation.NotEqual:
                    return (T)(object)!Evaluate<dynamic>(entity, expression.Left, contextInternal).Equals(Evaluate<dynamic>(entity, expression.Right, contextInternal));
                case ExpressionBinary.Operation.GreaterThan:
                    return (T)(object)(Evaluate<dynamic>(entity, expression.Left, contextInternal) > Evaluate<dynamic>(entity, expression.Right, contextInternal));
                case ExpressionBinary.Operation.GreaterThanOrEqual:
                    return (T)(object)(Evaluate<dynamic>(entity, expression.Left, contextInternal) >= Evaluate<dynamic>(entity, expression.Right, contextInternal));
                case ExpressionBinary.Operation.LessThan:
                    return (T)(object)(Evaluate<dynamic>(entity, expression.Left, contextInternal) < Evaluate<dynamic>(entity, expression.Right, contextInternal));
                case ExpressionBinary.Operation.LessThanOrEqual:
                    return (T)(object)(Evaluate<dynamic>(entity, expression.Left, contextInternal) <= Evaluate<dynamic>(entity, expression.Right, contextInternal));
                default:
                    throw new NotSupportedException($"Operation {expression.Op} is not supported.");
            }
        }
    }
}
