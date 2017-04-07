using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Semver;

namespace Spork.Services
{
    class Nuggieflector
    {
        readonly HttpClient _client = new HttpClient();

        public Nuggieflector()
        {
            // https://www.nuget.org/api/v2/package-versions/Rebus

            _client.BaseAddress = new Uri("https://www.nuget.org/api/v2/package-versions/");
        }

        public async Task<List<SemVersion>> GetVersions(string packageName)
        {
            var json = await _client.GetStringAsync(packageName);
            var versions = JsonConvert.DeserializeObject<string[]>(json);
            return versions
                .Select(version => SemVersion.Parse(version))
                .OrderBy(version => version)
                .ToList();
        }
    }
}