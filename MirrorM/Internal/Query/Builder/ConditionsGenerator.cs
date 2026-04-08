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
        private static bool IsExpressionTreeContainsField(Expression exp)
        {
            switch (exp)
            {
                case MemberExpression memberExpression:
                    var fieldInfo = memberExpression.Member.GetCustomAttribute<FieldAttribute>();

                    if (fieldInfo != null)
                        return true;

                    break;
                case BinaryExpression binaryExpression:
                    return IsExpressionTreeContainsField(binaryExpression.Left) || IsExpressionTreeContainsField(binaryExpression.Right);
                case UnaryExpression unaryExpression:
                    return IsExpressionTreeContainsField(unaryExpression.Operand);
                case MethodCallExpression methodCallExpression:
                    return IsExpressionTreeContainsField(methodCallExpression.Object) || methodCallExpression.Arguments.Any(a => IsExpressionTreeContainsField(a));
            }

            return false;
        }

        private static ExpressionBase Parse(Expression exp)
        {
            if (!IsExpressionTreeContainsField(exp))
                return new ExpressionConst(CalculateExpressionResult<object>(exp));

            switch (exp)
            {
                case BinaryExpression binaryExpression:
                    return ParseBinaryExpression(binaryExpression);
                case UnaryExpression unaryExpression:
                    return ParseUnaryExpression(unaryExpression);
                case ConstantExpression constExpression:
                    return new ExpressionConst(constExpression.Value);
                case MemberExpression memberExpression:
                    return ParseMemberExpression(memberExpression);
                case MethodCallExpression methodCallExpression:
                    return ParseMethodCallExpression(methodCallExpression);
                default:
                    throw new NotSupportedException($"Expression type {exp.NodeType} is not supported.");
            }
        }

        private static ExpressionBase ParseMemberExpression(MemberExpression memberExpression)
        {
            var fieldInfo = memberExpression.Member.GetCustomAttribute<FieldAttribute>();

            if (fieldInfo == null)
                return new ExpressionConst(CalculateExpressionResult<object>(memberExpression));

            return new ExpressionField(fieldInfo.FieldName);
        }

        private static T CalculateExpressionResult<T>(Expression expression)
        {
            return Expression.Lambda<Func<T>>(Expression.Convert(expression, typeof(T))).Compile()();
        }

        private static ExpressionBinary ParseBinaryExpression(BinaryExpression binaryExpression)
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

            if (binaryExpression.Left.Type.IsEnum && binaryExpression.Right.Type == typeof(int))
            {
                // .NET converts enum consts to int in expressions,
                // so we need to convert it back

                return new ExpressionBinary(
                    GetOp(),
                    Parse(binaryExpression.Left),
                    new ExpressionConst(Enum.ToObject(binaryExpression.Left.Type, CalculateExpressionResult<int>(binaryExpression.Right)))
                );
            }

            return new ExpressionBinary(
                GetOp(),
                Parse(binaryExpression.Left),
                Parse(binaryExpression.Right)
            );
        }

        private static ExpressionBase ParseMethodCallExpression(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.Name == nameof(Enumerable.Contains))
            {
                IEnumerable? enumerable = null;

                if (methodCallExpression.Arguments.Count == 2) // extension method
                {
                    enumerable = CalculateExpressionResult<IEnumerable>(methodCallExpression.Arguments[0]);
                }
                else if (methodCallExpression.Arguments.Count == 1) // member method
                {
                    enumerable = CalculateExpressionResult<IEnumerable>(methodCallExpression.Object!);
                }

                if (enumerable != null)
                {
                    return new ExpressionIn(enumerable.Cast<object>());
                }
            }

            throw new NotSupportedException($"Method {methodCallExpression.Method.Name} is not supported.");
        }

        private static ExpressionBase ParseUnaryExpression(UnaryExpression unaryExpression)
        {
            if (unaryExpression.NodeType == ExpressionType.Not)
            {
                return new ExpressionNot(Parse(unaryExpression.Operand));
            }

            throw new NotSupportedException($"Unary expression of type {unaryExpression.NodeType} is not supported.");
        }

        public static ExpressionBase GenerateFromPredicate<T>(Expression<Func<T, bool>> predicate) where T : Entity
        {
            var lambda = predicate as LambdaExpression;

            if (lambda == null)
                throw new NotSupportedException();

            return Parse(lambda.Body);
        }
    }
}
