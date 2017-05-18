using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Semver;

namespace Spork.Services
{
    class Nuggieflector : IDisposable
    {
        readonly HttpClient _client = new HttpClient();

        public Nuggieflector()
        {
            // https://www.nuget.org/api/v2/package-versions/Rebus

            _client.BaseAddress = new Uri("https://www.nuget.org/api/v2/package-versions/");
        }

        public async Task<List<SemVersion>> GetVersions(string packageName)
        {
            var relativeUrl = $"{packageName}?IncludePrerelease=true";
            var json = await _client.GetStringAsync(relativeUrl);
            var versions = JsonConvert.DeserializeObject<string[]>(json);
            var versionsList = versions
                .Select(version => SemVersion.Parse(version))
                .ToList();

            versionsList.Sort((v1,v2) => v1.CompareByPrecedence(v2));

            return versionsList;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}