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
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = DummySignature;

                Assert.Throws<BareRepositoryException>(() => repo.Stashes.Add(stasher, "My very first stash"));
            }
        }

        [Fact]
        public void CanAddAndRemoveStash()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
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
                repo.Stashes.Remove(0);
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Stashes.Add(null));
            }
        }

        [Fact]
        public void StashingAgainstCleanWorkDirShouldReturnANullStash()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
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
        [InlineData(-1)]
        [InlineData(-42)]
        public void RemovingStashWithBadParamShouldThrow(int badIndex)
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Stashes.Remove(badIndex));
            }
        }

        [Fact]
        public void CanGetStashByIndexer()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = DummySignature;
                const string firstStashMessage = "My very first stash";
                const string secondStashMessage = "My second stash";
                const string thirdStashMessage = "My third stash";

                // Create first stash
                Stash firstStash = repo.Stashes.Add(stasher, firstStashMessage, StashOptions.IncludeUntracked);
                Assert.NotNull(firstStash);

                // Create second stash
                string newFileFullPath = Path.Combine(repo.Info.WorkingDirectory, "stash_candidate.txt");
                File.WriteAllText(newFileFullPath, "Oh, I'm going to be stashed!\n");

                Stash secondStash = repo.Stashes.Add(stasher, secondStashMessage, StashOptions.IncludeUntracked);
                Assert.NotNull(secondStash);

                // Create third stash
                newFileFullPath = Path.Combine(repo.Info.WorkingDirectory, "stash_candidate_again.txt");
                File.WriteAllText(newFileFullPath, "Oh, I'm going to be stashed!\n");


                Stash thirdStash = repo.Stashes.Add(stasher, thirdStashMessage, StashOptions.IncludeUntracked);
                Assert.NotNull(thirdStash);

                // Get by indexer
                Assert.Equal(3, repo.Stashes.Count());
                Assert.Equal("stash@{0}", repo.Stashes[0].CanonicalName);
                Assert.Contains(thirdStashMessage, repo.Stashes[0].Message);
                Assert.Equal(thirdStash.Target, repo.Stashes[0].Target);
                Assert.Equal("stash@{1}", repo.Stashes[1].CanonicalName);
                Assert.Contains(secondStashMessage, repo.Stashes[1].Message);
                Assert.Equal(secondStash.Target, repo.Stashes[1].Target);
                Assert.Equal("stash@{2}", repo.Stashes[2].CanonicalName);
                Assert.Contains(firstStashMessage, repo.Stashes[2].Message);
                Assert.Equal(firstStash.Target, repo.Stashes[2].Target);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-42)]
        public void GettingStashWithBadIndexThrows(int badIndex)
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => repo.Stashes[badIndex]);
            }
        }

        [Theory]
        [InlineData(28)]
        [InlineData(42)]
        public void GettingAStashThatDoesNotExistReturnsNull(int bigIndex)
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Null(repo.Stashes[bigIndex]);
            }
        }
    }
}
