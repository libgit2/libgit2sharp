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
            //      Major.Minor.Patch[-somePreleaseTag]-libgit2_abbrev_hash (x86|x64 - features)
            // Example output:
            //      "0.17.0[-beta]+gdeadcafeee.LibGit2-06d772d (x86 - Threads, Https)"

            string versionInfo = GlobalSettings.Version.ToString();

            // The GlobalSettings.Version returned string should contain :
            //      version: '0.17.0[-somePreleaseTag]+[gSomeGitCommit.]LibGit2-06d772d' LibGit2Sharp version number.
            //      git2hash: '06d772d' LibGit2 library hash.
            //      arch: 'x86' or 'x64' LibGit2 target.
            //      git2Features: 'Threads, Ssh' LibGit2 features compiled with.
            string regex = @"^(?<version>\d+\.\d+\.\d+(-[\w\-\.]+)?\+(g(?<git2SharpHash>[a-f0-9]{10})\.)?LibGit2-[a-f0-9]{7}) \((?<arch>\w+) - (?<git2Features>(?:\w*(?:, )*\w+)*)\)$";

            Assert.NotNull(versionInfo);

            Match regexResult = Regex.Match(versionInfo, regex);

            Assert.True(regexResult.Success, "The following version string format is enforced:" +
                                             "Major.Minor.Patch[-somePreleaseTag]+[gSomeGitCommit].LibGit2-abbrev_hash (x86|x64 - features). " +
                                             "But found \"" + versionInfo + "\" instead.");
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
