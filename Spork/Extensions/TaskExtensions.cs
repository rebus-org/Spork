using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spork.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<List<TItem>> ToListAsync<TItem>(this IEnumerable<Task<TItem>> items)
        {
            var tasks = items.Select(i => i).ToArray();
            await Task.WhenAll(tasks);
            return tasks.Select(t => t.Result).ToList();
        }
    }
}