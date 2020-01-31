using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GoCommando;
using Octokit;
using Semver;
using Spinnerino;
using Spork.Extensions;
using Spork.Model;
using Spork.Services;
using Tababular;
// ReSharper disable RedundantAnonymousTypePropertyName
// ReSharper disable AccessToDisposedClosure

namespace Spork.Commands
{
    [Command("report")]
    [Description("Generates a full report of all Rebus projects")]
    public class GenerateReportCommand : ICommand
    {
        static readonly TableFormatter Formatter = new TableFormatter(new Hints { CollapseVerticallyWhenSingleLine = true });

        public void Run() => Execute().Wait();

        static async Task Execute()
        {
            var client = new GitHubClient(new ProductHeaderValue("spork-client"));

            using var repoflector = new Repoflector();

            using var nuggieflector = new Nuggieflector();

            var repositories = await client.Repository.GetAllForOrg("rebus-org");
            var rebusCoreVersion = (await nuggieflector.GetVersions("Rebus")).Last();

            Console.WriteLine("Loading repositories...");

            async Task<List<Dictionary<string, object>>> GetRows()
            {
                using var _ = new IndefiniteSpinner();

                return await GenerateReportCommand.GetRows(repositories, repoflector, nuggieflector, rebusCoreVersion);
            }

            var rows = await GetRows();

            Console.WriteLine(Formatter.FormatDictionaries(rows));
        }

        static async Task<List<Dictionary<string, object>>> GetRows(IReadOnlyList<Repository> repositories, Repoflector repoflector, Nuggieflector nuggieflector, SemVersion rebusCoreVersion)
        {
            var rows = await repositories
                .Where(repo => repo.IsSupportedRebusRepository())
                .OrderBy(repo => repo.Name)
                .Select(async repo =>
                {
                    var repositoryName = repo.Name;

                    var changeLogEntries = await repoflector.GetChangelog(repositoryName);
                    var nugetVersions = await nuggieflector.GetVersions(repositoryName);
                    var rebusDependencyVersion = await repoflector.GetRebusDependencyVersion(repositoryName);

                    var changelogVersion = changeLogEntries.LastOrDefault()?.Version;
                    var nugetStable = nugetVersions.LastOrDefault(v => string.IsNullOrWhiteSpace(v.Prerelease));
                    var nugetLatest = nugetVersions.LastOrDefault();

                    var needsRebusDependencyUpdate =
                        NeedsRebusDependencyUpdate(repositoryName, rebusCoreVersion, rebusDependencyVersion);
                    var needsPush = NeedsPush(changelogVersion, nugetLatest);

                    var openIssues = repo.OpenIssuesCount == 0 ? "" : repo.OpenIssuesCount.ToString();

                    return new Dictionary<string, object>
                    {
                        ["Repository"] = repositoryName,
                        ["Changelog ver."] = changelogVersion,
                        ["Rebus ver."] = rebusDependencyVersion?.VersionString,
                        ["Nuget stable"] = nugetStable,
                        ["Nuget latest"] = nugetLatest,
                        ["Rebus dep."] = needsRebusDependencyUpdate ? "!!!" : "",
                        ["Needs push"] = needsPush ? "!!!" : "",
                        ["Open issues"] = openIssues
                    };
                })
                .ToListAsync();
            return rows;
        }

        static bool NeedsPush(SemVersion changelogVersion, SemVersion nugetLatest)
        {
            return changelogVersion != nugetLatest;
        }

        static bool NeedsRebusDependencyUpdate(string repositoryName, SemVersion rebusCoreVersion, NuGetDependencyVersion rebusDependencyVersion)
        {
            if (repositoryName == "Rebus") return false;

            var rebusDependencySemver = rebusDependencyVersion.ToSemVersionOrNull();

            if (rebusDependencySemver != null && rebusCoreVersion != rebusDependencySemver)
            {
                return true;
            }

            return false;
        }
    }
}