using System.Linq;
using Octokit;

namespace Spork.Extensions
{
    public static class OctokitExtensions
    {
        static readonly string[] Include = {"Rebus"};
        static readonly string[] Exclude = {"Rebus.Hosting"};

        public static bool IsOfficialRebusRepository(this Repository repository)
        {
            var name = repository.Name;

            if (Exclude.Contains(name)) return false;

            if (Include.Contains(name)) return true;

            return name.StartsWith("Rebus.");
        }
    }
}