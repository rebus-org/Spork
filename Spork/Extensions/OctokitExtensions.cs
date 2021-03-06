﻿using Octokit;
using System.Linq;

namespace Spork.Extensions
{
    public static class OctokitExtensions
    {
        const string CoreRebusRepoName = "Rebus";

        static readonly string[] Include = {CoreRebusRepoName};
        static readonly string[] Exclude =
        {
            "Rebus.LegacyCompatibility",
            "Rebus.Recipes",
        };

        public static bool IsCore(this string repositoryName)
        {
            return string.Equals(CoreRebusRepoName, repositoryName);
        }

        public static bool IsSupportedRebusRepository(this Repository repository)
        {
            var name = repository.Name;
            var isDeprecated = (repository.Description??"").ToLowerInvariant().Contains("deprecated");

            if (isDeprecated) return false;

            if (Exclude.Contains(name)) return false;

            if (Include.Contains(name)) return true;

            return name.StartsWith("Rebus.");
        }
    }
}