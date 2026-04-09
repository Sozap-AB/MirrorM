namespace MirrorM.Tests.Tools
{
    internal static class IAsyncEnumerableExtensions
    {
        public static async Task<IList<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
        {
            var list = new List<T>();
            await foreach (var item in source)
            {
                list.Add(item);
            }
            return list;
        }
    }
}
