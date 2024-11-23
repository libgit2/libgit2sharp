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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = Constants.Signature;

                Assert.Throws<BareRepositoryException>(() => repo.Stashes.Add(stasher, "My very first stash", StashModifiers.Default));
            }
        }

        [Fact]
        public void CanAddAndRemoveStash()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = Constants.Signature;

                Assert.True(repo.RetrieveStatus().IsDirty);

                Stash stash = repo.Stashes.Add(stasher, "My very first stash", StashModifiers.IncludeUntracked);

                // Check that untracked files are deleted from working directory
                string untrackedFilename = "new_untracked_file.txt";
                Assert.False(File.Exists(Path.Combine(repo.Info.WorkingDirectory, untrackedFilename)));
                Assert.NotNull(stash.Untracked[untrackedFilename]);

                Assert.NotNull(stash);
                Assert.Equal("stash@{0}", stash.CanonicalName);
                Assert.Contains("My very first stash", stash.Message);

                var stashRef = repo.Refs["refs/stash"];
                Assert.Equal(stash.WorkTree.Sha, stashRef.TargetIdentifier);

                Assert.False(repo.RetrieveStatus().IsDirty);

                // Create extra file
                untrackedFilename = "stash_candidate.txt";
                Touch(repo.Info.WorkingDirectory, untrackedFilename, "Oh, I'm going to be stashed!\n");

                Stash secondStash = repo.Stashes.Add(stasher, "My second stash", StashModifiers.IncludeUntracked);

                Assert.NotNull(stash);
                Assert.Equal("stash@{0}", stash.CanonicalName);
                Assert.Contains("My second stash", secondStash.Message);

                Assert.Equal(2, repo.Stashes.Count());
                Assert.Equal("stash@{0}", repo.Stashes.First().CanonicalName);
                Assert.Equal("stash@{1}", repo.Stashes.Last().CanonicalName);

                Assert.NotNull(secondStash.Untracked[untrackedFilename]);

                // Stash history has been shifted
                Assert.Equal(repo.Lookup<Commit>("stash@{0}").Sha, secondStash.WorkTree.Sha);
                Assert.Equal(repo.Lookup<Commit>("stash@{1}").Sha, stash.WorkTree.Sha);

                //Remove one stash
                repo.Stashes.Remove(0);
                Assert.Single(repo.Stashes);
                Stash newTopStash = repo.Stashes.First();
                Assert.Equal("stash@{0}", newTopStash.CanonicalName);
                Assert.Equal(stash.WorkTree.Sha, newTopStash.WorkTree.Sha);

                // Stash history has been shifted
                Assert.Equal(stash.WorkTree.Sha, repo.Lookup<Commit>("stash").Sha);
                Assert.Equal(stash.WorkTree.Sha, repo.Lookup<Commit>("stash@{0}").Sha);
            }
        }

        [Fact]
        public void AddingAStashWithNoMessageGeneratesADefaultOne()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = Constants.Signature;

                Stash stash = repo.Stashes.Add(stasher, options: StashModifiers.Default);

                Assert.NotNull(stash);
                Assert.Equal("stash@{0}", stash.CanonicalName);
                Assert.NotEmpty(stash.WorkTree.Message);

                var stashRef = repo.Refs["refs/stash"];
                Assert.Equal(stash.WorkTree.Sha, stashRef.TargetIdentifier);
            }
        }

        [Fact]
        public void AddStashWithBadParamsShouldThrows()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Stashes.Add(default(Signature), options: StashModifiers.Default));
            }
        }

        [Fact]
        public void StashingAgainstCleanWorkDirShouldReturnANullStash()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = Constants.Signature;

                Stash stash = repo.Stashes.Add(stasher, "My very first stash", StashModifiers.IncludeUntracked);

                Assert.NotNull(stash);

                //Stash against clean working directory
                Assert.Null(repo.Stashes.Add(stasher, options: StashModifiers.Default));
            }
        }

        [Fact]
        public void CanStashWithoutOptions()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = Constants.Signature;

                const string untracked = "new_untracked_file.txt";
                Touch(repo.Info.WorkingDirectory, untracked, "I'm untracked\n");

                const string staged = "staged_file_path.txt";
                Touch(repo.Info.WorkingDirectory, staged, "I'm staged\n");
                Commands.Stage(repo, staged);

                Stash stash = repo.Stashes.Add(stasher, "Stash with default options", StashModifiers.Default);

                Assert.NotNull(stash);

                //It should not keep staged files
                Assert.Equal(FileStatus.Nonexistent, repo.RetrieveStatus(staged));
                Assert.NotNull(stash.Index[staged]);

                //It should leave untracked files untracked
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus(untracked));
                Assert.Null(stash.Untracked);
            }
        }

        [Fact]
        public void CanStashAndKeepIndex()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = Constants.Signature;

                const string filename = "staged_file_path.txt";
                Touch(repo.Info.WorkingDirectory, filename, "I'm staged\n");
                Commands.Stage(repo, filename);

                Stash stash = repo.Stashes.Add(stasher, "This stash will keep index", StashModifiers.KeepIndex);

                Assert.NotNull(stash);
                Assert.NotNull(stash.Index[filename]);
                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(filename));
                Assert.Null(stash.Untracked);
            }
        }

        [Fact]
        public void CanStashIgnoredFiles()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string gitIgnore = ".gitignore";
                const string ignoredFilename = "ignored_file.txt";

                Touch(repo.Info.WorkingDirectory, gitIgnore, ignoredFilename);
                Commands.Stage(repo, gitIgnore);
                repo.Commit("Modify gitignore", Constants.Signature, Constants.Signature);

                Touch(repo.Info.WorkingDirectory, ignoredFilename, "I'm ignored\n");

                Assert.True(repo.Ignore.IsPathIgnored(ignoredFilename));

                var stasher = Constants.Signature;
                var stash = repo.Stashes.Add(stasher, "This stash includes ignore files", StashModifiers.IncludeIgnored);

                Assert.False(File.Exists(Path.Combine(repo.Info.WorkingDirectory, ignoredFilename)));

                var blob = repo.Lookup<Blob>("stash^3:ignored_file.txt");
                Assert.NotNull(blob);
                Assert.NotNull(stash.Untracked[ignoredFilename]);
            }
        }

        [Fact]
        public void CanStashAndApplyWithOptions()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = Constants.Signature;

                const string filename = "staged_file_path.txt";
                Touch(repo.Info.WorkingDirectory, filename, "I'm staged\n");
                Commands.Stage(repo, filename);

                repo.Stashes.Add(stasher, "This stash with default options");
                Assert.Equal(StashApplyStatus.Applied, repo.Stashes.Apply(0));

                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(filename));
                Assert.Single(repo.Stashes);

                Commands.Stage(repo, filename);

                repo.Stashes.Add(stasher, "This stash with default options");
                Assert.Equal(StashApplyStatus.Applied, repo.Stashes.Apply(
                    0,
                    new StashApplyOptions
                    {
                        ApplyModifiers = StashApplyModifiers.ReinstateIndex,
                    }));

                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(filename));
                Assert.Equal(2, repo.Stashes.Count());
            }
        }

        [Fact]
        public void CanStashAndPop()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = Constants.Signature;

                Assert.Empty(repo.Stashes);

                const string filename = "staged_file_path.txt";
                const string contents = "I'm staged";
                Touch(repo.Info.WorkingDirectory, filename, contents);
                Commands.Stage(repo, filename);

                repo.Stashes.Add(stasher, "This stash with default options");
                Assert.Single(repo.Stashes);

                Assert.Equal(StashApplyStatus.Applied, repo.Stashes.Pop(0));
                Assert.Empty(repo.Stashes);

                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus(filename));
                Assert.Equal(contents, File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, filename)));
            }
        }

        [Fact]
        public void StashFailsWithUncommittedChangesIntheIndex()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = Constants.Signature;

                const string filename = "staged_file_path.txt";
                const string originalContents = "I'm pre-stash.";
                const string filename2 = "unstaged_file_path.txt";
                const string newContents = "I'm post-stash.";

                Touch(repo.Info.WorkingDirectory, filename, originalContents);
                Commands.Stage(repo, filename);
                Touch(repo.Info.WorkingDirectory, filename2, originalContents);

                repo.Stashes.Add(stasher, "This stash with default options");

                Touch(repo.Info.WorkingDirectory, filename, newContents);
                Commands.Stage(repo, filename);
                Touch(repo.Info.WorkingDirectory, filename2, newContents);

                Assert.Equal(StashApplyStatus.UncommittedChanges, repo.Stashes.Pop(0, new StashApplyOptions
                {
                    ApplyModifiers = StashApplyModifiers.ReinstateIndex,
                }));
                Assert.Single(repo.Stashes);
                Assert.Equal(newContents, File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, filename)));
                Assert.Equal(newContents, File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, filename2)));
            }
        }

        [Fact]
        public void StashCallsTheCallback()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = Constants.Signature;
                bool called;

                const string filename = "staged_file_path.txt";
                const string filename2 = "unstaged_file_path.txt";
                const string originalContents = "I'm pre-stash.";

                Touch(repo.Info.WorkingDirectory, filename, originalContents);
                Commands.Stage(repo, filename);
                Touch(repo.Info.WorkingDirectory, filename2, originalContents);

                repo.Stashes.Add(stasher, "This stash with default options");

                called = false;
                repo.Stashes.Apply(0, new StashApplyOptions
                {
                    ProgressHandler = (progress) => { called = true; return true; }
                });

                Assert.True(called);

                repo.Reset(ResetMode.Hard);

                called = false;
                repo.Stashes.Pop(0, new StashApplyOptions
                {
                    ProgressHandler = (progress) => { called = true; return true; }
                });

                Assert.True(called);
            }
        }

        [Fact]
        public void StashApplyReportsNotFound()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = Constants.Signature;

                const string filename = "unstaged_file_path.txt";
                Touch(repo.Info.WorkingDirectory, filename, "I'm unstaged\n");

                repo.Stashes.Add(stasher, "This stash with default options", StashModifiers.IncludeUntracked);
                Touch(repo.Info.WorkingDirectory, filename, "I'm another unstaged\n");

                Assert.Equal(StashApplyStatus.NotFound, repo.Stashes.Pop(1));
                Assert.Throws<ArgumentException>(() => repo.Stashes.Pop(-1));
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-42)]
        public void RemovingStashWithBadParamShouldThrow(int badIndex)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Stashes.Remove(badIndex));
            }
        }

        [Fact]
        public void CanGetStashByIndexer()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var stasher = Constants.Signature;
                const string firstStashMessage = "My very first stash";
                const string secondStashMessage = "My second stash";
                const string thirdStashMessage = "My third stash";

                // Create first stash
                Stash firstStash = repo.Stashes.Add(stasher, firstStashMessage, StashModifiers.IncludeUntracked);
                Assert.NotNull(firstStash);

                // Create second stash
                Touch(repo.Info.WorkingDirectory, "stash_candidate.txt", "Oh, I'm going to be stashed!\n");

                Stash secondStash = repo.Stashes.Add(stasher, secondStashMessage, StashModifiers.IncludeUntracked);
                Assert.NotNull(secondStash);

                // Create third stash
                Touch(repo.Info.WorkingDirectory, "stash_candidate_again.txt", "Oh, I'm going to be stashed!\n");


                Stash thirdStash = repo.Stashes.Add(stasher, thirdStashMessage, StashModifiers.IncludeUntracked);
                Assert.NotNull(thirdStash);

                // Get by indexer
                Assert.Equal(3, repo.Stashes.Count());
                Assert.Equal("stash@{0}", repo.Stashes[0].CanonicalName);
                Assert.Contains(thirdStashMessage, repo.Stashes[0].Message);
                Assert.Equal(thirdStash.WorkTree, repo.Stashes[0].WorkTree);
                Assert.Equal("stash@{1}", repo.Stashes[1].CanonicalName);
                Assert.Contains(secondStashMessage, repo.Stashes[1].Message);
                Assert.Equal(secondStash.WorkTree, repo.Stashes[1].WorkTree);
                Assert.Equal("stash@{2}", repo.Stashes[2].CanonicalName);
                Assert.Contains(firstStashMessage, repo.Stashes[2].Message);
                Assert.Equal(firstStash.WorkTree, repo.Stashes[2].WorkTree);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-42)]
        public void GettingStashWithBadIndexThrows(int badIndex)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => repo.Stashes[badIndex]);
            }
        }

        [Theory]
        [InlineData(28)]
        [InlineData(42)]
        public void GettingAStashThatDoesNotExistReturnsNull(int bigIndex)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Null(repo.Stashes[bigIndex]);
            }
        }
    }
}
