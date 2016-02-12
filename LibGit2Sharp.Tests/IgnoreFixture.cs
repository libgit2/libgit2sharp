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
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, "Foo.cs", "Bar");

                Assert.True(repo.RetrieveStatus().Untracked.Select(s => s.FilePath).Contains("Foo.cs"));

                repo.Ignore.AddTemporaryRules(new[] { "*.cs" });

                Assert.False(repo.RetrieveStatus().Untracked.Select(s => s.FilePath).Contains("Foo.cs"));

                repo.Ignore.ResetAllTemporaryRules();

                Assert.True(repo.RetrieveStatus().Untracked.Select(s => s.FilePath).Contains("Foo.cs"));
            }
        }

        [Fact]
        public void IsPathIgnoredShouldVerifyWhetherPathIsIgnored()
        {
            string path = SandboxStandardTestRepo();
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
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Ignore.IsPathIgnored(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Ignore.IsPathIgnored(null));
            }
        }

        [Fact]
        public void AddingATemporaryRuleWithBadParamsThrows()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Ignore.AddTemporaryRules(null));
            }
        }

        [Fact]
        public void CanCheckIfAPathIsIgnoredUsingThePreferedPlatformDirectorySeparatorChar()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory, ".gitignore", "/NewFolder\n/NewFolder/NewFolder");

                Assert.False(repo.Ignore.IsPathIgnored("File.txt"));
                Assert.True(repo.Ignore.IsPathIgnored("NewFolder"));
                Assert.True(repo.Ignore.IsPathIgnored(string.Format(@"NewFolder{0}NewFolder", Path.DirectorySeparatorChar)));
                Assert.True(repo.Ignore.IsPathIgnored(string.Format(@"NewFolder{0}NewFolder{0}File.txt", Path.DirectorySeparatorChar)));
            }
        }

        [Fact]
        public void HonorDeeplyNestedGitIgnoreFile()
        {
            string path = InitNewRepository();
            using (var repo = new Repository(path))
            {
                char pd = Path.DirectorySeparatorChar;

                var gitIgnoreFile = string.Format("deeply{0}nested{0}.gitignore", pd);
                Touch(repo.Info.WorkingDirectory, gitIgnoreFile, "SmtCounters.h");

                repo.Stage(gitIgnoreFile);
                repo.Commit("Add .gitignore", Constants.Signature, Constants.Signature);

                Assert.False(repo.RetrieveStatus().IsDirty);

                var ignoredFile = string.Format("deeply{0}nested{0}SmtCounters.h", pd);
                Touch(repo.Info.WorkingDirectory, ignoredFile, "Content");
                Assert.False(repo.RetrieveStatus().IsDirty);

                var file = string.Format("deeply{0}nested{0}file.txt", pd);
                Touch(repo.Info.WorkingDirectory, file, "Yeah!");

                var repositoryStatus = repo.RetrieveStatus();
                Assert.True(repositoryStatus.IsDirty);

                Assert.Equal(FileStatus.Ignored, repositoryStatus[ignoredFile].State);
                Assert.Equal(FileStatus.NewInWorkdir, repositoryStatus[file].State);

                Assert.True(repo.Ignore.IsPathIgnored(ignoredFile));
                Assert.False(repo.Ignore.IsPathIgnored(file));
            }
        }
    }
}
