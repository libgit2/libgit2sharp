using System;
using System.IO;
using System.Reflection;
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
            //      Major.Minor.Patch[-previewTag]+libgit2-{libgit2_abbrev_hash}.{LibGit2Sharp_hash} (arch - features)
            // Example output:
            //      "0.27.0-preview.0.1896+libgit2-c058aa8.c1ac3ed74487da5fac24cf1e48dc8ea71e917b75 (x64 - Threads, Https, NSec)"

            string versionInfo = GlobalSettings.Version.ToString();

            // The GlobalSettings.Version returned string should contain :
            //      version: '0.25.0[-previewTag]' LibGit2Sharp version number.
            //      git2SharpHash: 'c1ac3ed74487da5fac24cf1e48dc8ea71e917b75' LibGit2Sharp hash.
            //      arch: 'x86' or 'x64' libgit2 target.
            //      git2Features: 'Threads, Ssh' libgit2 features compiled with.
            string regex = @"^(?<version>\d+\.\d+\.\d+(-[\w\-\.]+)?)\+libgit2-[a-f0-9]{7}\.((?<git2SharpHash>[a-f0-9]{40}))? \((?<arch>\w+) - (?<git2Features>(?:\w*(?:, )*\w+)*)\)$";

            Assert.NotNull(versionInfo);

            Match regexResult = Regex.Match(versionInfo, regex);

            Assert.True(regexResult.Success, "The following version string format is enforced:" +
                                             "Major.Minor.Patch[-previewTag]+libgit2-{libgit2_abbrev_hash}.{LibGit2Sharp_hash} (arch - features). " +
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
            var platformDir = Path.Combine(tempDir, "plat", architecture);
            var libraryPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lib", "win32", architecture);

            try
            {
                Directory.CreateDirectory(platformDir);
                File.Copy(Path.Combine(libraryPath, nativeDllFileName), Path.Combine(platformDir, nativeDllFileName));

                var (output, exitCode) = ProcessHelper.RunProcess(testAppExe, arguments: $@"{NativeDllName.Name} ""{platformDir}""", workingDirectory: tempDir);

                Assert.Empty(output);
                Assert.Equal(0, exitCode);
            }
            finally
            {
                DirectoryHelper.DeleteDirectory(tempDir);
            }
        }

        [Fact]
        public void SetExtensions()
        {
            var extensions = GlobalSettings.GetExtensions();

            // Assert that "noop" is supported by default
            Assert.Equal(new[] { "noop", "objectformat" }, extensions);

            // Disable "noop" extensions
            GlobalSettings.SetExtensions("!noop");
            extensions = GlobalSettings.GetExtensions();
            Assert.Equal(new[] { "objectformat" }, extensions);

            // Enable two new extensions (it will reset the configuration and "noop" will be enabled)
            GlobalSettings.SetExtensions("partialclone", "newext");
            extensions = GlobalSettings.GetExtensions();
            Assert.Equal(new[] { "noop", "objectformat", "partialclone", "newext" }, extensions);
        }
    }
}
