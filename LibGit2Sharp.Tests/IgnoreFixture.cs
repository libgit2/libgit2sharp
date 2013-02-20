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
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, "Foo.cs"), "Bar");

                Assert.True(repo.Index.RetrieveStatus().Untracked.Contains("Foo.cs"));

                repo.Ignore.AddTemporaryRules(new[] { "*.cs" });

                Assert.False(repo.Index.RetrieveStatus().Untracked.Contains("Foo.cs"));

                repo.Ignore.ResetAllTemporaryRules();

                Assert.True(repo.Index.RetrieveStatus().Untracked.Contains("Foo.cs"));
            }
        }

        [Fact]
        public void IsPathIgnoredShouldVerifyWhetherPathIsIgnored()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, "Foo.cs"), "Bar");

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
    }
}
