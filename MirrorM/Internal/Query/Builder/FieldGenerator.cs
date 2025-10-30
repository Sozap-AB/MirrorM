using FishingTourServer.Sys.Services.Data.Database.Attributes;
using MirrorM.AdapterInterface.Query;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MirrorM.Internal.Query.Builder
{
    internal static class FieldGenerator
    {
        public static Field GenerateFromExpression<T, R>(Expression<Func<T, R>> selector)
        {
            MemberExpression memberExpression = (MemberExpression)selector.Body;

            var fieldInfo = memberExpression.Member.GetCustomAttribute<FieldAttribute>();

            if (fieldInfo == null)
                throw new InvalidOperationException("Field selector is not selecting property annotated with FieldAttribute");

            return new Field(fieldInfo.FieldName);
        }
    }
}
