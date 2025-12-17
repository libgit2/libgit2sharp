using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class ReflogFixture : BaseFixture
    {
        [Fact]
        public void CanReadReflog()
        {
            const int expectedReflogEntriesCount = 3;

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var reflog = repo.Refs.Log(repo.Refs.Head);

                Assert.Equal(expectedReflogEntriesCount, reflog.Count());

                // Initial commit assertions
                Assert.Equal("timothy.clem@gmail.com", reflog.Last().Committer.Email);
                Assert.StartsWith("clone: from", reflog.Last().Message);
                Assert.Equal(ObjectId.Zero, reflog.Last().From);

                // second commit assertions
                Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", reflog.ElementAt(expectedReflogEntriesCount - 2).From.Sha);
                Assert.Equal("592d3c869dbc4127fc57c189cb94f2794fa84e7e", reflog.ElementAt(expectedReflogEntriesCount - 2).To.Sha);
            }
        }

        [Fact]
        public void ReflogOfUnbornReferenceIsEmpty()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Empty(repo.Refs.Log("refs/heads/toto"));
            }
        }

        [Fact]
        public void ReadingReflogOfInvalidReferenceNameThrows()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<InvalidSpecificationException>(() => repo.Refs.Log("toto").Count());
            }
        }

        [Fact]
        public void CommitShouldCreateReflogEntryOnHeadAndOnTargetedDirectReference()
        {
            string repoPath = InitNewRepository();

            var identity = Constants.Identity;

            using (var repo = new Repository(repoPath, new RepositoryOptions { Identity = identity }))
            {
                // setup refs as HEAD => unit_test => master
                var newRef = repo.Refs.Add("refs/heads/unit_test", "refs/heads/master");
                Assert.NotNull(newRef);
                repo.Refs.UpdateTarget(repo.Refs.Head, newRef);

                const string relativeFilepath = "new.txt";
                Touch(repo.Info.WorkingDirectory, relativeFilepath, "content\n");
                Commands.Stage(repo, relativeFilepath);

                var author = Constants.Signature;
                const string commitMessage = "Hope reflog behaves as it should";

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Commit commit = repo.Commit(commitMessage, author, author);

                // Assert a reflog entry is created on HEAD
                Assert.Single(repo.Refs.Log("HEAD"));
                var reflogEntry = repo.Refs.Log("HEAD").First();

                Assert.Equal(identity.Name, reflogEntry.Committer.Name);
                Assert.Equal(identity.Email, reflogEntry.Committer.Email);

                // When verifying the timestamp range, give a little more room on the range.
                // Git or file system datetime truncation seems to cause these stamps to jump up to a second earlier
                // than we expect. See https://github.com/libgit2/libgit2sharp/issues/1764
                var low = before - TimeSpan.FromSeconds(1);
                var high = DateTimeOffset.Now.TruncateMilliseconds() + TimeSpan.FromSeconds(1);
                Assert.InRange(reflogEntry.Committer.When, low, high);

                Assert.Equal(commit.Id, reflogEntry.To);
                Assert.Equal(ObjectId.Zero, reflogEntry.From);

                // Assert the same reflog entry is created on refs/heads/master
                Assert.Single(repo.Refs.Log("refs/heads/master"));
                reflogEntry = repo.Refs.Log("HEAD").First();

                Assert.Equal(identity.Name, reflogEntry.Committer.Name);
                Assert.Equal(identity.Email, reflogEntry.Committer.Email);

                Assert.InRange(reflogEntry.Committer.When, low, high);

                Assert.Equal(commit.Id, reflogEntry.To);
                Assert.Equal(ObjectId.Zero, reflogEntry.From);

                // Assert no reflog entry is created on refs/heads/unit_test
                Assert.Empty(repo.Refs.Log("refs/heads/unit_test"));
            }
        }

        [Fact]
        public void CommitOnUnbornReferenceShouldCreateReflogEntryWithInitialTag()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                const string relativeFilepath = "new.txt";
                Touch(repo.Info.WorkingDirectory, relativeFilepath, "content\n");
                Commands.Stage(repo, relativeFilepath);

                var author = Constants.Signature;
                const string commitMessage = "First commit should be logged as initial";
                repo.Commit(commitMessage, author, author);

                // Assert the reflog entry message is correct
                Assert.Single(repo.Refs.Log("HEAD"));
                Assert.Equal(string.Format("commit (initial): {0}", commitMessage), repo.Refs.Log("HEAD").First().Message);
            }
        }

        [Fact]
        public void CommitOnDetachedHeadShouldInsertReflogEntry()
        {
            string repoPath = SandboxStandardTestRepo();

            var identity = Constants.Identity;

            using (var repo = new Repository(repoPath, new RepositoryOptions { Identity = identity }))
            {
                Assert.False(repo.Info.IsHeadDetached);

                var parentCommit = repo.Head.Tip.Parents.First();
                Commands.Checkout(repo, parentCommit.Sha);
                Assert.True(repo.Info.IsHeadDetached);

                const string relativeFilepath = "new.txt";
                Touch(repo.Info.WorkingDirectory, relativeFilepath, "content\n");
                Commands.Stage(repo, relativeFilepath);

                var author = Constants.Signature;
                const string commitMessage = "Commit on detached head";

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                var commit = repo.Commit(commitMessage, author, author);

                // Assert a reflog entry is created on HEAD
                var reflogEntry = repo.Refs.Log("HEAD").First();

                Assert.Equal(identity.Name, reflogEntry.Committer.Name);
                Assert.Equal(identity.Email, reflogEntry.Committer.Email);

                // When verifying the timestamp range, give a little more room on the range.
                // Git or file system datetime truncation seems to cause these stamps to jump up to a second earlier
                // than we expect. See https://github.com/libgit2/libgit2sharp/issues/1764
                var low = before - TimeSpan.FromSeconds(1);
                var high = DateTimeOffset.Now.TruncateMilliseconds() + TimeSpan.FromSeconds(1);
                Assert.InRange(reflogEntry.Committer.When, low, high);

                Assert.Equal(commit.Id, reflogEntry.To);
                Assert.Equal(string.Format("commit: {0}", commitMessage), repo.Refs.Log("HEAD").First().Message);
            }
        }

        [Theory]
        [InlineData(false, null, true)]
        [InlineData(false, true, true)]
        [InlineData(true, true, true)]
        [InlineData(true, null, false)]
        [InlineData(true, false, false)]
        [InlineData(false, false, false)]
        public void AppendingToReflogDependsOnCoreLogAllRefUpdatesSetting(bool isBare, bool? setting, bool expectAppend)
        {
            var repoPath = InitNewRepository(isBare);

            using (var repo = new Repository(repoPath))
            {
                if (setting != null)
                {
                    EnableRefLog(repo, setting.Value);
                }

                var blob = repo.ObjectDatabase.CreateBlob(Stream.Null);
                var tree = repo.ObjectDatabase.CreateTree(new TreeDefinition().Add("yoink", blob, Mode.NonExecutableFile));
                var commit = repo.ObjectDatabase.CreateCommit(Constants.Signature, Constants.Signature, "yoink",
                                                 tree, Enumerable.Empty<Commit>(), false);

                var branch = repo.CreateBranch("yoink", commit);
                var log = repo.Refs.Log(branch.CanonicalName);

                Assert.Equal(expectAppend ? 1 : 0, log.Count());
            }
        }

        [Fact]
        public void UnsignedMethodsWriteCorrectlyToTheReflog()
        {
            var repoPath = InitNewRepository(true);
            using (var repo = new Repository(repoPath, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                var blob = repo.ObjectDatabase.CreateBlob(Stream.Null);
                var tree = repo.ObjectDatabase.CreateTree(new TreeDefinition().Add("yoink", blob, Mode.NonExecutableFile));
                var commit = repo.ObjectDatabase.CreateCommit(Constants.Signature, Constants.Signature, "yoink",
                                                 tree, Enumerable.Empty<Commit>(), false);

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                var direct = repo.Refs.Add("refs/heads/direct", commit.Id);
                AssertRefLogEntry(repo, direct.CanonicalName, null, null,
                    direct.ResolveToDirectReference().Target.Id, Constants.Identity, before);

                var symbolic = repo.Refs.Add("refs/heads/symbolic", direct);
                Assert.Empty(repo.Refs.Log(symbolic)); // creation of symbolic refs doesn't update the reflog
            }
        }
    }
}
