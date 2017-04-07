using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Semver;
using Spork.Extensions;
using Spork.Model;

namespace Spork.Services
{
    class Repoflector
    {
        readonly HttpClient _client = new HttpClient();
        readonly ChangelogParser _changelogParser = new ChangelogParser();

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
                var changelogString = await _client.GetStringAsync(relativeAddress);
                var changeLogEntries = _changelogParser.ParseChangelog(changelogString).ToList();
                return changeLogEntries;
            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Could not get CHANGELOG.md from {_client.BaseAddress}{relativeAddress}", exception);
            }
        }

        public async Task<SemVersion> GetRebusDependencyVersion(string repositoryName)
        {
            if (repositoryName.IsCore()) return null;

            // https://github.com/rebus-org/Rebus.Msmq/blob/master/Rebus.Msmq/Rebus.Msmq.csproj

            var relativeAddress = $"{repositoryName}/master/{repositoryName}/{repositoryName}.csproj";

            try
            {
                var mainProjectFileXml = await _client.GetStringAsync(relativeAddress);

                return GetRebusVersionFrom(mainProjectFileXml);
            }
            catch (Exception exception)
            {
                throw new ApplicationException($"Could not get <main-project>.csproj from {_client.BaseAddress}{relativeAddress}", exception);
            }
        }

        SemVersion GetRebusVersionFrom(string mainProjectFileXml)
        {
            var document = XDocument.Parse(mainProjectFileXml);

            // look for this:
            // <PackageReference Include="Rebus" Version="4.0.0-b06" />

            var projectElement = document.Element("Project");

            if (projectElement == null) return null;

            var rebusPackageReference = projectElement
                .Elements().SelectMany(g => g.Elements().Where(e => e.Name == "PackageReference"))
                .FirstOrDefault(e => e.HasAttributes && string.Equals(e.Attribute("Include")?.Value, "Rebus", StringComparison.OrdinalIgnoreCase));

            var semVerVersionString = rebusPackageReference?.Attribute("Version")?.Value;

            return semVerVersionString == null ? null : SemVersion.Parse(semVerVersionString);
        }
    }
}