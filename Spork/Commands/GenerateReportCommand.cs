using System;
using System.Linq;
using System.Threading.Tasks;
using GoCommando;
using Octokit;
using Semver;
using Spinnerino;
using Spork.Extensions;
using Spork.Services;
using Tababular;
// ReSharper disable RedundantAnonymousTypePropertyName

namespace Spork.Commands
{
    [Command("report")]
    [Description("Generates a full report of all Rebus projects")]
    public class GenerateReportCommand : ICommand
    {
        static readonly TableFormatter Formatter = new TableFormatter(new Hints { CollapseVerticallyWhenSingleLine = true });

        public void Run()
        {
            Execute().Wait();
        }

        async Task Execute()
        {
            var client = new GitHubClient(new ProductHeaderValue("spork-client"));
            var repoflector = new Repoflector();
            var nuggieflector = new Nuggieflector();

            var repositories = await client.Repository.GetAllForOrg("rebus-org");
            var rebusCoreVersion = (await nuggieflector.GetVersions("Rebus")).Last();

            Console.WriteLine("Loading repositories...");

            var spinner = new IndefiniteSpinner();
            var rows = await repositories
                .Where(repo => repo.IsOfficialRebusRepository())
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

                    var isSpiffy = IsSpiffy(repositoryName, rebusCoreVersion, rebusDependencyVersion);

                    return new
                    {
                        Name = repositoryName,
                        ChangelogVersion = changelogVersion,
                        RebusVersion = rebusDependencyVersion,
                        NugetStable = nugetStable,
                        NugetLatest = nugetLatest,
                        IsSpiffy = isSpiffy
                    };
                })
                .ToListAsync();

            spinner.Dispose();

            Console.WriteLine(Formatter.FormatObjects(rows));
        }

        bool IsSpiffy(string repositoryName, SemVersion rebusCoreVersion, SemVersion rebusDependencyVersion)
        {
            if (repositoryName == "Rebus") return true;

            if (rebusCoreVersion != rebusDependencyVersion)
            {
                return false;
            }

            return true;
        }
    }
}