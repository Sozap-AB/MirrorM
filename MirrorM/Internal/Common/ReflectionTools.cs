using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MirrorM.Internal.Common
{
    internal static class ReflectionTools
    {
        public static PropertyInfo GetPropertyFromSelector(
            Expression selector
        )
        {
            Expression body = selector;
            if (body is LambdaExpression expression)
            {
                body = expression.Body;
            }
            switch (body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return (PropertyInfo)((MemberExpression)body).Member;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
