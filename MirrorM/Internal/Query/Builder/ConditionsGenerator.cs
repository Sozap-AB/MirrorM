using MirrorM.AdapterInterface.Query.Conditions;
using MirrorM.Attributes;
using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MirrorM.Internal.Query.Builder
{
    internal static class ConditionsGenerator
    {
        private static bool IsExpressionTreeContainsField(Expression entity, Expression exp)
        {
            switch (exp)
            {
                case MemberExpression memberExpression:
                    if (memberExpression.Expression != entity)
                        return false;

                    var fieldInfo = memberExpression.Member.GetCustomAttribute<FieldAttribute>();

                    if (fieldInfo != null)
                        return true;

                    break;
                case BinaryExpression binaryExpression:
                    return IsExpressionTreeContainsField(entity, binaryExpression.Left) || IsExpressionTreeContainsField(entity, binaryExpression.Right);
                case UnaryExpression unaryExpression:
                    return IsExpressionTreeContainsField(entity, unaryExpression.Operand);
                case MethodCallExpression methodCallExpression:
                    return IsExpressionTreeContainsField(entity, methodCallExpression.Object) || methodCallExpression.Arguments.Any(a => IsExpressionTreeContainsField(entity, a));
            }

            return false;
        }

        private static ExpressionBase Parse(Expression entity, Expression exp)
        {
            if (!IsExpressionTreeContainsField(entity, exp))
                return new ExpressionConst(CalculateExpressionResult<object>(exp));

            switch (exp)
            {
                case BinaryExpression binaryExpression:
                    return ParseBinaryExpression(entity, binaryExpression);
                case UnaryExpression unaryExpression:
                    return ParseUnaryExpression(entity, unaryExpression);
                case ConstantExpression constExpression:
                    return new ExpressionConst(constExpression.Value);
                case MemberExpression memberExpression:
                    return ParseMemberExpression(entity, memberExpression);
                case MethodCallExpression methodCallExpression:
                    return ParseMethodCallExpression(entity, methodCallExpression);
                default:
                    throw new NotSupportedException($"Expression type {exp.NodeType} is not supported.");
            }
        }

        private static ExpressionBase ParseMemberExpression(Expression entity, MemberExpression memberExpression)
        {
            var fieldInfo = memberExpression.Member.GetCustomAttribute<FieldAttribute>();

            if (fieldInfo == null || memberExpression.Expression != entity)
                return new ExpressionConst(CalculateExpressionResult<object>(memberExpression));

            return new ExpressionField(fieldInfo.FieldName);
        }

        private static T CalculateExpressionResult<T>(Expression expression)
        {
            return Expression.Lambda<Func<T>>(Expression.Convert(expression, typeof(T))).Compile()();
        }

        private static ExpressionBinary ParseBinaryExpression(Expression entity, BinaryExpression binaryExpression)
        {
            ExpressionBinary.Operation GetOp()
            {
                switch (binaryExpression.NodeType)
                {
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                        return ExpressionBinary.Operation.And;
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                        return ExpressionBinary.Operation.Or;
                    case ExpressionType.Add:
                        return ExpressionBinary.Operation.Add;
                    case ExpressionType.Subtract:
                        return ExpressionBinary.Operation.Subtract;
                    case ExpressionType.Multiply:
                        return ExpressionBinary.Operation.Multiply;
                    case ExpressionType.Divide:
                        return ExpressionBinary.Operation.Divide;
                    case ExpressionType.Equal:
                        return ExpressionBinary.Operation.Equal;
                    case ExpressionType.NotEqual:
                        return ExpressionBinary.Operation.NotEqual;
                    case ExpressionType.GreaterThan:
                        return ExpressionBinary.Operation.GreaterThan;
                    case ExpressionType.GreaterThanOrEqual:
                        return ExpressionBinary.Operation.GreaterThanOrEqual;
                    case ExpressionType.LessThan:
                        return ExpressionBinary.Operation.LessThan;
                    case ExpressionType.LessThanOrEqual:
                        return ExpressionBinary.Operation.LessThanOrEqual;
                    default:
                        throw new NotSupportedException($"Expression type {binaryExpression.NodeType} is not supported.");
                }
            }

            if (binaryExpression.Left.NodeType == ExpressionType.Convert &&
                binaryExpression.Left is UnaryExpression lue &&
                lue.Operand.Type.IsEnum &&
                binaryExpression.Right.Type == typeof(int))
            {
                // .NET converts enum consts to int in expressions,
                // so we need to convert it back

                return new ExpressionBinary(
                    GetOp(),
                    Parse(entity, lue.Operand),
                    new ExpressionConst(Enum.ToObject(lue.Operand.Type, CalculateExpressionResult<int>(binaryExpression.Right)))
                );
            }

            return new ExpressionBinary(
                GetOp(),
                Parse(entity, binaryExpression.Left),
                Parse(entity, binaryExpression.Right)
            );
        }

        private static ExpressionBase ParseMethodCallExpression(Expression entity, MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.Name == nameof(Enumerable.Contains))
            {
                IEnumerable? enumerable = null;
                ExpressionBase? value = null;

                if (methodCallExpression.Arguments.Count == 2) // extension method
                {
                    value = Parse(entity, methodCallExpression.Arguments[1]);
                    enumerable = CalculateExpressionResult<IEnumerable>(methodCallExpression.Arguments[0]);
                }
                else if (methodCallExpression.Arguments.Count == 1) // member method
                {
                    value = Parse(entity, methodCallExpression.Arguments[0]);
                    enumerable = CalculateExpressionResult<IEnumerable>(methodCallExpression.Object!);
                }

                if (enumerable != null)
                {
                    return new ExpressionIn(value!, enumerable.Cast<object>());
                }
            }

            throw new NotSupportedException($"Method {methodCallExpression.Method.Name} is not supported.");
        }

        private static ExpressionBase ParseUnaryExpression(Expression entity, UnaryExpression unaryExpression)
        {
            if (unaryExpression.NodeType == ExpressionType.Not)
            {
                return new ExpressionNot(Parse(entity, unaryExpression.Operand));
            }

            throw new NotSupportedException($"Unary expression of type {unaryExpression.NodeType} is not supported.");
        }

        public static ExpressionBase GenerateFromPredicate<T>(Expression<Func<T, bool>> predicate) where T : EntityBase
        {
            LambdaExpression lambda = (predicate as LambdaExpression) ?? throw new NotSupportedException();
            ParameterExpression argument = lambda.Parameters.SingleOrDefault(p => p.Type == typeof(T)) ?? throw new NotSupportedException();

            return Parse(argument, lambda.Body);
        }
    }
}
