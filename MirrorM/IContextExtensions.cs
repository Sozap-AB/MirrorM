using System;
using System.Threading.Tasks;

namespace MirrorM
{
    public static class IContextExtensions
    {
        public static async Task<T> GetByIdAsync<T>(this IContext context, Guid entityId) where T : Entity
        {
            return (await context.GetOptionalByIdAsync<T>(entityId))!;
        }

        public static Task<T?> GetOptionalByIdAsync<T>(this IContext context, Guid entityId) where T : Entity
        {
            return context.Query<T>().Where(e => e.Id == entityId).FirstOrDefaultAsync();
        }
    }
}
