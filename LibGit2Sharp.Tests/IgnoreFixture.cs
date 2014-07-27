using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class IgnoreFixture : BaseFixture
    {
        [Fact]
        public void TemporaryRulesShouldApplyUntilCleared()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, "Foo.cs", "Bar");

                Assert.True(repo.Index.RetrieveStatus().Untracked.Select(s => s.FilePath).Contains("Foo.cs"));

                repo.Ignore.AddTemporaryRules(new[] { "*.cs" });

                Assert.False(repo.Index.RetrieveStatus().Untracked.Select(s => s.FilePath).Contains("Foo.cs"));

                repo.Ignore.ResetAllTemporaryRules();

                Assert.True(repo.Index.RetrieveStatus().Untracked.Select(s => s.FilePath).Contains("Foo.cs"));
            }
        }

        [Fact]
        public void IsPathIgnoredShouldVerifyWhetherPathIsIgnored()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, "Foo.cs", "Bar");

                Assert.False(repo.Ignore.IsPathIgnored("Foo.cs"));

                repo.Ignore.AddTemporaryRules(new[] { "*.cs" });

                Assert.True(repo.Ignore.IsPathIgnored("Foo.cs"));

                repo.Ignore.ResetAllTemporaryRules();

                Assert.False(repo.Ignore.IsPathIgnored("Foo.cs"));
            }
        }

        [Fact]
        public void CallingIsPathIgnoredWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Ignore.IsPathIgnored(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Ignore.IsPathIgnored(null));
            }
        }

        [Fact]
        public void AddingATemporaryRuleWithBadParamsThrows()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Ignore.AddTemporaryRules(null));
            }
        }

        [Fact]
        public void CanCheckIfAPathIsIgnoredUsingThePreferedPlatformDirectorySeparatorChar()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, ".gitignore", "/NewFolder\n/NewFolder/NewFolder");

                Assert.False(repo.Ignore.IsPathIgnored("File.txt"));
                Assert.True(repo.Ignore.IsPathIgnored("NewFolder"));
                Assert.True(repo.Ignore.IsPathIgnored(string.Format(@"NewFolder{0}NewFolder", Path.DirectorySeparatorChar)));
                Assert.True(repo.Ignore.IsPathIgnored(string.Format(@"NewFolder{0}NewFolder{0}File.txt", Path.DirectorySeparatorChar)));
            }
        }
    }
}
