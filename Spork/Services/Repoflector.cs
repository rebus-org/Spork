using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nito.AsyncEx;
using Spork.Extensions;
using Spork.Model;

namespace Spork.Services
{
    class Repoflector : IDisposable
    {
        readonly ConcurrentDictionary<string, string> _fileCache = new(StringComparer.OrdinalIgnoreCase);
        readonly ConcurrentDictionary<string, AsyncSemaphore> _cacheSemaphores = new();
        readonly ChangelogParser _changelogParser = new();
        readonly HttpClient _client = new();

        public Repoflector()
        {
            _client.BaseAddress = new Uri("https://raw.githubusercontent.com/rebus-org/");
        }

        public async Task<List<ChangeLogEntry>> GetChangelog(string repositoryName)
        {
            // https://raw.githubusercontent.com/rebus-org/Rebus/master/CHANGELOG.md

            var relativeAddress = $"{repositoryName}/master/CHANGELOG.md";

            try
            {
                var changelogString = await GetFileFromUrl(relativeAddress);

                var changeLogEntries = _changelogParser.ParseChangelog(changelogString).ToList();

                return changeLogEntries;
            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Could not get CHANGELOG.md from {_client.BaseAddress}{relativeAddress}", exception);
            }
        }

        public async Task<NuGetDependencyVersion> GetRebusDependencyVersion(string repositoryName)
        {
            if (repositoryName.IsCore()) return null;

            // https://github.com/rebus-org/Rebus.Msmq/blob/master/Rebus.Msmq/Rebus.Msmq.csproj

            var relativeAddress = $"{repositoryName}/master/{repositoryName}/{repositoryName}.csproj";

            try
            {
                var mainProjectFileXml = await GetFileFromUrl(relativeAddress);

                return GetRebusVersionFrom(mainProjectFileXml);
            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Could not get <main-project>.csproj from {_client.BaseAddress}{relativeAddress}", exception);
            }
        }

        public async Task<string> GetLicenseExpression(string repositoryName)
        {
            // https://github.com/rebus-org/Rebus.Msmq/blob/master/Rebus.Msmq/Rebus.Msmq.csproj

            var relativeAddress = $"{repositoryName}/master/{repositoryName}/{repositoryName}.csproj";

            try
            {
                var mainProjectFileXml = await GetFileFromUrl(relativeAddress);
                var document = XDocument.Parse(mainProjectFileXml);
                var projectElement = document.Element("Project");
                if (projectElement == null) return null;

                var licenseExpressionElement = projectElement
                    .Elements().SelectMany(g => g.Elements())
                    .FirstOrDefault(e => e.Name == "PackageLicenseExpression");

                return licenseExpressionElement?.Value;
            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Could not get <main-project>.csproj from {_client.BaseAddress}{relativeAddress}", exception);
            }
        }

        public async Task<string> GetPackageIcon(string repositoryName)
        {
            // https://github.com/rebus-org/Rebus.Msmq/blob/master/Rebus.Msmq/Rebus.Msmq.csproj

            var relativeAddress = $"{repositoryName}/master/{repositoryName}/{repositoryName}.csproj";

            try
            {
                var mainProjectFileXml = await GetFileFromUrl(relativeAddress);
                var document = XDocument.Parse(mainProjectFileXml);
                var projectElement = document.Element("Project");
                if (projectElement == null) return null;

                var packageIconElement = projectElement
                    .Elements().SelectMany(g => g.Elements())
                    .FirstOrDefault(e => e.Name == "PackageIcon");

                return packageIconElement?.Value;
            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Could not get <main-project>.csproj from {_client.BaseAddress}{relativeAddress}", exception);
            }
        }

        public async Task<string> GetPackageReadmePath(string repositoryName)
        {
            // https://github.com/rebus-org/Rebus.Msmq/blob/master/Rebus.Msmq/Rebus.Msmq.csproj

            var relativeAddress = $"{repositoryName}/master/{repositoryName}/{repositoryName}.csproj";

            try
            {
                var mainProjectFileXml = await GetFileFromUrl(relativeAddress);
                var document = XDocument.Parse(mainProjectFileXml);
                var projectElement = document.Element("Project");
                if (projectElement == null) return null;

                var packageIconElement = projectElement
                    .Elements().SelectMany(g => g.Elements())
                    .FirstOrDefault(e => e.Name == "PackageIcon");

                return packageIconElement?.Value;
            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Could not get <main-project>.csproj from {_client.BaseAddress}{relativeAddress}", exception);
            }
        }

        static NuGetDependencyVersion GetRebusVersionFrom(string mainProjectFileXml)
        {
            var document = XDocument.Parse(mainProjectFileXml);

            // look for this:
            // <PackageReference Include="Rebus" Version="4.0.0-b06" />
            // or this:
            // <PackageReference Include="Rebus" Version="4.0.0-*" />

            var projectElement = document.Element("Project");

            if (projectElement == null) return null;

            var rebusPackageReference = projectElement
                .Elements().SelectMany(g => g.Elements().Where(e => e.Name == "PackageReference"))
                .FirstOrDefault(e => e.HasAttributes && string.Equals(e.Attribute("Include")?.Value, "Rebus", StringComparison.OrdinalIgnoreCase));

            var semVerVersionString = rebusPackageReference?.Attribute("Version")?.Value;

            return new NuGetDependencyVersion(semVerVersionString);
        }

        async Task<string> GetFileFromUrl(string relativeAddress)
        {
            if (_fileCache.TryGetValue(relativeAddress, out var result)) return result;

            using var _ = await _cacheSemaphores.GetOrAdd(relativeAddress, x => new(initialCount: 1)).LockAsync();

            if (_fileCache.TryGetValue(relativeAddress, out var result2)) return result2;

            using var response = await _client.GetAsync(relativeAddress);

            response.EnsureSuccessStatusCode();

            var str = await response.Content.ReadAsStringAsync();

            _fileCache[relativeAddress] = str;

            return str;
        }

        public void Dispose() => _client?.Dispose();
    }
}