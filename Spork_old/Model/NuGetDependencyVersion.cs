using Semver;

namespace Spork.Model
{
    public class NuGetDependencyVersion
    {
        public string VersionString { get; }

        public NuGetDependencyVersion(string versionString)
        {
            VersionString = versionString;
        }

        public bool HasVersion => !string.IsNullOrWhiteSpace(VersionString);

        public SemVersion ToSemVersionOrNull()
        {
            return SemVersion.TryParse(VersionString, out var version)
                ? version
                : null;
        }
    }
}