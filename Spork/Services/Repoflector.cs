using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Spork.Model;

namespace Spork.Services
{
    class Repoflector
    {
        readonly HttpClient _client = new HttpClient();
        readonly ChangelogParser _changelogParser = new ChangelogParser();

        public Repoflector()
        {
            // https://raw.githubusercontent.com/rebus-org/Rebus/master/CHANGELOG.md

            _client.BaseAddress = new Uri("https://raw.githubusercontent.com/rebus-org/");
        }

        public async Task<List<ChangeLogEntry>> GetChangelog(string repositoryName)
        {
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
    }
}