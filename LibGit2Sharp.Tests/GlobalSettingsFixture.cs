using System;
using System.IO;
using System.Text.RegularExpressions;
using LibGit2Sharp.Core;
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
            //      Major.Minor.Patch[-previewTag]+{LibGit2Sharp_abbrev_hash}.libgit2-{libgit2_abbrev_hash} (x86|x64 - features)
            // Example output:
            //      "0.25.0-preview.52+871d13a67f.libgit2-15e1193 (x86 - Threads, Https)"

            string versionInfo = GlobalSettings.Version.ToString();

            // The GlobalSettings.Version returned string should contain :
            //      version: '0.25.0[-previewTag]' LibGit2Sharp version number.
            //      git2SharpHash: '871d13a67f' LibGit2Sharp hash.
            //      arch: 'x86' or 'x64' libgit2 target.
            //      git2Features: 'Threads, Ssh' libgit2 features compiled with.
            string regex = @"^(?<version>\d+\.\d+\.\d+(-[\w\-\.]+)?\+((?<git2SharpHash>[a-f0-9]{10})\.)?libgit2-[a-f0-9]{7}) \((?<arch>\w+) - (?<git2Features>(?:\w*(?:, )*\w+)*)\)$";

            Assert.NotNull(versionInfo);

            Match regexResult = Regex.Match(versionInfo, regex);

            Assert.True(regexResult.Success, "The following version string format is enforced:" +
                                             "Major.Minor.Patch[-previewTag]+{LibGit2Sharp_abbrev_hash}.libgit2-{libgit2_abbrev_hash} (x86|x64 - features). " +
                                             "But found \"" + versionInfo + "\" instead.");
        }

        [Fact]
        public void TryingToResetNativeLibraryPathAfterLoadedThrows()
        {
            // Do something that loads the native library
            var features = GlobalSettings.Version.Features;

            Assert.Throws<LibGit2SharpException>(() => { GlobalSettings.NativeLibraryPath = "C:/Foo"; });
        }

        [SkippableTheory]
        [InlineData("x86")]
        [InlineData("x64")]
        public void LoadFromSpecifiedPath(string architecture)
        {
            Skip.IfNot(Platform.IsRunningOnNetFramework(), ".NET Framework only test.");

            var nativeDllFileName = NativeDllName.Name + ".dll";
            var testDir = Path.GetDirectoryName(typeof(GlobalSettingsFixture).Assembly.Location);
            var testAppExe = Path.Combine(testDir, $"NativeLibraryLoadTestApp.{architecture}.exe");
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var platformDir = Path.Combine(tempDir, "plat");

            try
            {
                Directory.CreateDirectory(Path.Combine(platformDir, architecture));
                File.Copy(Path.Combine(GlobalSettings.NativeLibraryPath, architecture, nativeDllFileName), Path.Combine(platformDir, architecture, nativeDllFileName));

                var (output, exitCode) = ProcessHelper.RunProcess(testAppExe, arguments: $@"{NativeDllName.Name} ""{platformDir}""", workingDirectory: tempDir);

                Assert.Empty(output);
                Assert.Equal(0, exitCode);
            }
            finally
            {
                DirectoryHelper.DeleteDirectory(tempDir);
            }
        }
    }
}
