using System;
using System.Collections.Generic;
using System.Linq;
using Semver;
using Spork.Extensions;

namespace Spork.Model
{
    class ChangelogParser
    {
        public IEnumerable<ChangeLogEntry> ParseChangelog(string changelog)
        {
            #region Example

            /*
## 0.99.73
* Add GZIPping capability to data bus storage - can be enabled by attaching `.UseCompression()` in the data bus configuration builder
* Factor forwarding of failed messages to error queues out into `PoisonQueueErrorHandler` which implements `IErrorHandler`. Make room for customizing what to do about failed messages.
## 0.99.74
* Mark assemblies as CLS compliant becase VB.NET and F# programmers are most welcome too - thanks [NKnusperer]
* Update Serilog dependency to 2.1.0 - thanks [NKnusperer]
* Limit number of workers to match max parallelism
* Make thread pool-based workers default (old strategy can still be had by calling `o.UseClassicRebusWorkersMessageDispatch()`)
* Update NLog dependency to 4.3.7 - thanks [SvenVandenbrande]
* Update SimpleInjector dependency to 3.2.0 - thanks [SvenVandenbrande]
* Make adjustment to new thread pool-based workers that makes better use of async receive APIs of transports
* Update Wire dependency to 0.8.0
* Update Autofac dependency to 4.0.1
* Fix bug in Amazon SQS transport that would cause it to be unable to receive messages if the last created queue was not the transport's own input queue
## 2.0.0-a2
* Improve SQL transport expired messages cleanup to hit an index - thanks [xenoputtss]
## 2.0.0-a7
* Update to .NET 4.5.2 because it is the lowest framework version currently supported by Microsoft
## 2.0.0-a8
* Update NUnit dependency to 3.4.1
## 2.0.0-a9
* Fix file-based lock which was kept for longer than necessary (i.e. until GC would collect the `FileStream` that had not been properly disposed)
## 2.0.0-a10
* Experimentally multi-targeting .NET 4.5, 4.5.2, 4.6, and 4.6.1 (but it dit NOT work for 4.6 and 4.6.1)
             */

            #endregion

            var entriesText = TrimHeaderAndFooter(changelog);

            return entriesText.Split(new[] {"##"}, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseEntryOrNull)
                .Where(entry => entry != null);
        }

        static string TrimHeaderAndFooter(string changelog)
        {
            var indexOfFirstEntry = changelog.IndexOf("##");
            if (indexOfFirstEntry < 0) throw new FormatException("Could not find any entries in changelog");

            var withoutHeader = changelog.Substring(indexOfFirstEntry);
            var indexOfFooterMark = withoutHeader.IndexOf("---");
            if (indexOfFooterMark <= 0) return withoutHeader;

            var withoutFooter = withoutHeader.Substring(0, indexOfFooterMark - 1);
            return withoutFooter;
        }

        static ChangeLogEntry ParseEntryOrNull(string entriesText)
        {
            SemVersion Version(string versionString)
            {
                try
                {
                    return SemVersion.Parse(versionString);
                }
                catch (Exception exception)
                {
                    throw new FormatException($"Cannot parse the string '{versionString}' into a SemVer", exception);
                }
            }
            /*
 2.0.0-a1
* Test release
            */

            try
            {
                var lines = entriesText.GetLines();
                var versionString = lines.First().Trim();

                if (versionString.StartsWith("<")) return null;
                if (versionString.StartsWith("~~")) return null; // strike-through of version means that it must be ignored

                var version = Version(versionString);
                var bulletLines = lines.Skip(1);
                var bullets = bulletLines
                    .Select(text => text.Trim())
                    .Where(text => text.StartsWith("*"))
                    .Select(text => text.Substring(1).Trim())
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToArray();

                if (bullets.Length == 0)
                {
                    throw new FormatException($"No bullets for version {versionString}");
                }

                return new ChangeLogEntry(version, bullets);
            }
            catch (Exception exception)
            {
                throw new FormatException($@"Invalid changelog entry format:
<entry>
{entriesText}
</entry>
", exception);
            }
        }
    }
}