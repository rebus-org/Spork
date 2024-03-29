﻿using System;
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
// ReSharper disable UnusedMember.Global

namespace Spork.Commands
{
    [Command("report")]
    [Description("Generates a full report of all Rebus projects")]
    public class GenerateReportCommand : ICommand
    {
        static readonly TableFormatter Formatter = new(new Hints { CollapseVerticallyWhenSingleLine = true });

        public void Run() => Execute().Wait();

        static async Task Execute()
        {
            var client = new GitHubClient(new ProductHeaderValue("spork-client"));

            using var repoflector = new Repoflector();

            using var nuggieflector = new Nuggieflector();

            var repositories = await client.Repository.GetAllForOrg("rebus-org");
            var rebusCoreVersion = (await nuggieflector.GetVersions("Rebus")).Last();

            Console.WriteLine("Loading repositories...");

            async Task<List<Dictionary<string, object>>> GetRowsWithSpinner()
            {
                using var _ = new IndefiniteSpinner();

                return await GetRows(repositories, repoflector, nuggieflector, rebusCoreVersion);
            }

            var rows = await GetRowsWithSpinner();

            Console.WriteLine(Formatter.FormatDictionaries(rows));

            Console.WriteLine(string.Join(Environment.NewLine, AllPredicaments.Select(p => $"    {p.ShortHand}: {p.Description}")));
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
                    var licenseExpression = await repoflector.GetLicenseExpression(repositoryName);
                    var packageIconPath = await repoflector.GetPackageIcon(repositoryName);
                    var packageReadmeFile = await repoflector.GetPackageReadmePath(repositoryName);

                    var changelogVersion = changeLogEntries.LastOrDefault()?.Version;
                    var nugetStable = nugetVersions.LastOrDefault(v => string.IsNullOrWhiteSpace(v.Prerelease));
                    var nugetLatest = nugetVersions.LastOrDefault();

                    var needsRebusDependencyUpdate = NeedsRebusDependencyUpdate(repositoryName, rebusCoreVersion, rebusDependencyVersion);

                    var predicaments = new[]
                    {
                        changelogVersion != nugetLatest ? ChangelogAndNugetNotInSync : null,
                        licenseExpression != "MIT" ? WrongLicenseExpression : null,
                        string.IsNullOrWhiteSpace(packageIconPath) ? MissingPackageIcon : null,
                        string.IsNullOrWhiteSpace(packageReadmeFile) ? MissingPackageReadme : null,
                    };

                    var openIssues = repo.OpenIssuesCount == 0 ? "" : repo.OpenIssuesCount.ToString();

                    return new Dictionary<string, object>
                    {
                        ["Repository"] = repositoryName,
                        ["Changelog ver."] = changelogVersion,
                        ["Rebus ver."] = rebusDependencyVersion?.VersionString,
                        ["Nuget stable"] = nugetStable,
                        ["Nuget latest"] = nugetLatest,
                        ["Rebus dep."] = needsRebusDependencyUpdate,
                        ["Open issues"] = openIssues,
                        [""] = string.Join(" ", predicaments.Where(p => p != null).Select(p => p.ShortHand)),
                    };
                })
                .ToListAsync();

            return rows;
        }

        record Predicament(string ShortHand, string Description);

        static readonly Predicament ChangelogAndNugetNotInSync = new("V", "Newest entry in changelog does not match latest in NuGet");
        static readonly Predicament WrongLicenseExpression = new("L", "License expression is not MIT");
        static readonly Predicament MissingPackageIcon = new("I", "Missing package icon");
        static readonly Predicament MissingPackageReadme = new("R", "Missing package README");

        static readonly IReadOnlyList<Predicament> AllPredicaments = new[]
        {
            ChangelogAndNugetNotInSync,
            WrongLicenseExpression,
            MissingPackageIcon,
            MissingPackageReadme
        };

        static bool NeedsPush(SemVersion changelogVersion, SemVersion nugetLatest)
        {
            return changelogVersion != nugetLatest;
        }

        static string NeedsRebusDependencyUpdate(string repositoryName, SemVersion rebusCoreVersion, NuGetDependencyVersion rebusDependencyVersion)
        {
            try
            {
                if (repositoryName == "Rebus") return "";

                var rebusDependencySemver = rebusDependencyVersion.ToSemVersionOrNull();

                if (rebusDependencySemver != null && rebusCoreVersion != rebusDependencySemver)
                {
                    return $"{rebusCoreVersion} != {rebusDependencySemver}";
                }

                return "";
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }
    }
}