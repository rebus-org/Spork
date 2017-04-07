using System;
using System.Linq;
using System.Threading.Tasks;
using GoCommando;
using Octokit;
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
            var repositories = await client.Repository.GetAllForOrg("rebus-org");
            var repoflector = new Repoflector();
            var nuggieflector = new Nuggieflector();

            Console.WriteLine("Loading repositories...");
            var spinner = new IndefiniteSpinner();
            var rows = await repositories
                .Where(repo => repo.IsOfficialRebusRepository())
                .OrderBy(repo => repo.Name)
                .Select(async repo =>
                {
                    var changeLogEntries = await repoflector.GetChangelog(repo.Name);
                    var nugetVersions = await nuggieflector.GetVersions(repo.Name);

                    return new
                    {
                        Name = repo.Name,
                        ChangelogVersion = changeLogEntries.LastOrDefault()?.Version,
                        NugetVersion = nugetVersions.LastOrDefault()
                    };
                })
                .ToListAsync();

            spinner.Dispose();

            Console.WriteLine(Formatter.FormatObjects(rows));
        }
    }
}