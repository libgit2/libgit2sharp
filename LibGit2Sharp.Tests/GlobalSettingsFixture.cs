using System;
using System.Text.RegularExpressions;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class GlobalSettingsFixture : BaseFixture
    {
        [Fact]
        public void CanGetMinimumCompiledInFeatures()
        {
            BuiltInFeatures features = GlobalSettings.Version.Features;

            Assert.True(features.HasFlag(BuiltInFeatures.Threads));
            Assert.True(features.HasFlag(BuiltInFeatures.Https));
        }

        [Fact]
        public void CanRetrieveValidVersionString()
        {
            // Version string format is:
            //      Major.Minor.Patch-LibGit2Sharp_abbrev_hash-libgit2_abbrev_hash (x86|amd64 - features)
            // Example output:
            //      "0.17.0-unknown-06d772d (x86 - Threads, Https)"

            string versionInfo = GlobalSettings.Version.ToString();

            // The GlobalSettings.Version returned string should contain :
            //      version: '0.17.0[.198[-pre]]' LibGit2Sharp version number.
            //      git2SharpHash:'unknown' ( when compiled from source ) else LibGit2Sharp library hash.
            //      git2hash: '06d772d' LibGit2 library hash.
            //      arch: 'x86' or 'amd64' LibGit2 target.
            //      git2Features: 'Threads, Ssh' LibGit2 features compiled with.
            string regex = @"^(?<version>\d{1,}\.\d{1,2}\.\d{1,3}(\.\d{1,5}(-pre)?)?)-(?<git2SharpHash>\w+)-(?<git2Hash>\w+) \((?<arch>\w+) - (?<git2Features>(?:\w*(?:, )*\w+)*)\)$";

            Assert.NotNull(versionInfo);

            Match regexResult = Regex.Match(versionInfo, regex);

            Assert.True(regexResult.Success, "The following version string format is enforced:" +
                                             "Major.Minor.Patch[.Build['-pre']]-LibGit2Sharp_abbrev_hash-libgit2_abbrev_hash (x86|amd64 - features)");

            GroupCollection matchGroups = regexResult.Groups;

            // Check that all groups are valid
            for (int i = 0; i < matchGroups.Count; i++)
            {
                if (i == 1 || i == 2) // Build number and '-pre' are optional
                {
                    continue;
                }

                Assert.True(matchGroups[i].Success);
            }
        }
    }
}
