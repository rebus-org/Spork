using System.Linq;
using Octokit;

namespace Spork.Extensions
{
    public static class OctokitExtensions
    {
        const string CoreRebusRepoName = "Rebus";

        static readonly string[] Include = {CoreRebusRepoName};
        static readonly string[] Exclude =
        {
            "Rebus.Hosting",
            "Rebus.LegacyCompatibility",
            "Rebus.Recipes",
        };

        public static bool IsCore(this string repositoryName)
        {
            return string.Equals(CoreRebusRepoName, repositoryName);
        }

        public static bool IsOfficialRebusRepository(this Repository repository)
        {
            var name = repository.Name;

            if (Exclude.Contains(name)) return false;

            if (Include.Contains(name)) return true;

            return name.StartsWith("Rebus.");
        }
    }
}