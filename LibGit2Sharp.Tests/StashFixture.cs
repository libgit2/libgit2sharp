using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class StashFixture : BaseFixture
    {
        [Fact]
        public void CannotAddStashAgainstBareRepository()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();
            using (var repo = new Repository(path.RepositoryPath))
            {
                var stasher = DummySignature;

                Assert.Throws<BareRepositoryException>(() => repo.Stashes.Add(stasher, "My very first stash"));
            }
        }

        [Fact]
        public void CanAddAndRemoveStash()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                var stasher = DummySignature;

                Assert.True(repo.Index.RetrieveStatus().IsDirty);

                Stash stash = repo.Stashes.Add(stasher, "My very first stash", StashOptions.IncludeUntracked);

                // Check that untracked files are deleted from working directory
                Assert.False(File.Exists(Path.Combine(repo.Info.WorkingDirectory, "new_untracked_file.txt")));

                Assert.NotNull(stash);
                Assert.Equal("stash@{0}", stash.CanonicalName);
                Assert.Contains("My very first stash", stash.Message);

                var stashRef = repo.Refs["refs/stash"];
                Assert.Equal(stash.Target.Sha, stashRef.TargetIdentifier);

                Assert.False(repo.Index.RetrieveStatus().IsDirty);

                // Create extra file
                string newFileFullPath = Path.Combine(repo.Info.WorkingDirectory, "stash_candidate.txt");
                File.WriteAllText(newFileFullPath, "Oh, I'm going to be stashed!\n");

                Stash secondStash = repo.Stashes.Add(stasher, "My second stash", StashOptions.IncludeUntracked);

                Assert.NotNull(stash);
                Assert.Equal("stash@{0}", stash.CanonicalName);
                Assert.Contains("My second stash", secondStash.Message);

                Assert.Equal(2, repo.Stashes.Count());
                Assert.Equal("stash@{0}", repo.Stashes.First().CanonicalName);
                Assert.Equal("stash@{1}", repo.Stashes.Last().CanonicalName);

                // Stash history has been shifted
                Assert.Equal(repo.Lookup<Commit>("stash@{0}").Sha, secondStash.Target.Sha);
                Assert.Equal(repo.Lookup<Commit>("stash@{1}").Sha, stash.Target.Sha);

                //Remove one stash
                repo.Stashes.Remove("stash@{0}");
                Assert.Equal(1, repo.Stashes.Count());
                Stash newTopStash = repo.Stashes.First();
                Assert.Equal("stash@{0}", newTopStash.CanonicalName);
                Assert.Equal(stash.Target.Sha, newTopStash.Target.Sha);

                // Stash history has been shifted
                Assert.Equal(stash.Target.Sha, repo.Lookup<Commit>("stash").Sha);
                Assert.Equal(stash.Target.Sha, repo.Lookup<Commit>("stash@{0}").Sha);
            }
        }

        [Fact]
        public void AddingAStashWithNoMessageGeneratesADefaultOne()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                var stasher = DummySignature;

                Stash stash = repo.Stashes.Add(stasher);

                Assert.NotNull(stash);
                Assert.Equal("stash@{0}", stash.CanonicalName);
                Assert.NotEmpty(stash.Target.Message);

                var stashRef = repo.Refs["refs/stash"];
                Assert.Equal(stash.Target.Sha, stashRef.TargetIdentifier);
            }
        }

        [Fact]
        public void AddStashWithBadParamsShouldThrows()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Stashes.Add(null));
            }
        }

        [Fact]
        public void StashingAgainstCleanWorkDirShouldReturnANullStash()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                var stasher = DummySignature;

                Stash stash = repo.Stashes.Add(stasher, "My very first stash", StashOptions.IncludeUntracked);

                Assert.NotNull(stash);

                //Stash against clean working directory
                Assert.Null(repo.Stashes.Add(stasher));
            }
        }

        [Fact]
        public void CanStashWithoutOptions()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                var stasher = DummySignature;

                var untrackedFilePath = Path.Combine(repo.Info.WorkingDirectory, "new_untracked_file.txt");
                File.WriteAllText(untrackedFilePath, "I'm untracked\n");

                string stagedfilePath = Path.Combine(repo.Info.WorkingDirectory, "staged_file_path.txt");
                File.WriteAllText(stagedfilePath, "I'm staged\n");
                repo.Index.Stage(stagedfilePath);

                Stash stash = repo.Stashes.Add(stasher, "Stash with default options");

                Assert.NotNull(stash);

                //It should not keep staged files
                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus("staged_file_path.txt"));

                //It should leave untracked files untracked
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus("new_untracked_file.txt"));
            }
        }

        [Fact]
        public void CanStashAndKeepIndex()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                var stasher = DummySignature;

                string stagedfilePath = Path.Combine(repo.Info.WorkingDirectory, "staged_file_path.txt");
                File.WriteAllText(stagedfilePath, "I'm staged\n");
                repo.Index.Stage(stagedfilePath);

                Stash stash = repo.Stashes.Add(stasher, "This stash wil keep index", StashOptions.KeepIndex);

                Assert.NotNull(stash);
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus("staged_file_path.txt"));
            }
        }

        [Fact]
        public void CanStashIgnoredFiles()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);

            using (var repo = new Repository(path.RepositoryPath))
            {
                string gitIgnoreFilePath = Path.Combine(repo.Info.WorkingDirectory, ".gitignore");
                File.WriteAllText(gitIgnoreFilePath, "ignored_file.txt");
                repo.Index.Stage(gitIgnoreFilePath);
                repo.Commit("Modify gitignore", Constants.Signature, Constants.Signature);

                string ignoredFilePath = Path.Combine(repo.Info.WorkingDirectory, "ignored_file.txt");
                File.WriteAllText(ignoredFilePath, "I'm ignored\n");

                Assert.True(repo.Ignore.IsPathIgnored("ignored_file.txt"));

                var stasher = DummySignature;
                repo.Stashes.Add(stasher, "This stash includes ignore files", StashOptions.IncludeIgnored);

                //TODO : below assertion doesn't pass. Bug?
                //Assert.False(File.Exists(ignoredFilePath));

                var blob = repo.Lookup<Blob>("stash^3:ignored_file.txt");
                Assert.NotNull(blob);
            }
        }

        [Theory]
        [InlineData("stah@{0}")]
        [InlineData("stash@{0")]
        [InlineData("stash@{fake}")]
        [InlineData("dummy")]
        public void RemovingStashWithBadParamShouldThrow(string stashRefLog)
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Stashes.Remove(stashRefLog));
            }
        }
    }
}
