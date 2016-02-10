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
            //      Major.Minor.Patch[-preDateTime]-LibGit2Sharp_abbrev_hash-libgit2_abbrev_hash (x86|amd64 - features)
            // Example output:
            //      "0.17.0[-pre20170914123547]-deadcafe-06d772d (x86 - Threads, Https)"

            string versionInfo = GlobalSettings.Version.ToString();

            // The GlobalSettings.Version returned string should contain :
            //      version: '0.17.0[-pre20170914123547]' LibGit2Sharp version number.
            //      git2SharpHash:'unknown' ( when compiled from source ) else LibGit2Sharp library hash.
            //      git2hash: '06d772d' LibGit2 library hash.
            //      arch: 'x86' or 'amd64' LibGit2 target.
            //      git2Features: 'Threads, Ssh' LibGit2 features compiled with.
            string regex = @"^(?<version>\d{1,}\.\d{1,2}\.\d{1,3}(-(pre|dev)\d{14})?)-(?<git2SharpHash>\w+)-(?<git2Hash>\w+) \((?<arch>\w+) - (?<git2Features>(?:\w*(?:, )*\w+)*)\)$";

            Assert.NotNull(versionInfo);

            Match regexResult = Regex.Match(versionInfo, regex);

            Assert.True(regexResult.Success, "The following version string format is enforced:" +
                                             "Major.Minor.Patch[-preDateTime]-LibGit2Sharp_abbrev_hash-libgit2_abbrev_hash (x86|amd64 - features)");

            GroupCollection matchGroups = regexResult.Groups;

            Assert.Equal(8, matchGroups.Count);

            // Check that all groups are valid
            for (int i = 0; i < matchGroups.Count; i++)
            {
                if (i == 1 || i == 2) // '-pre' segment is optional
                {
                    continue;
                }

                Assert.True(matchGroups[i].Success);
            }
        }

        [Fact]
        public void TryingToResetNativeLibraryPathAfterLoadedThrows()
        {
            // Do something that loads the native library
            Assert.NotNull(GlobalSettings.Version.Features);

            Assert.Throws<LibGit2SharpException>(() => { GlobalSettings.NativeLibraryPath = "C:/Foo"; });
        }
    }
}
