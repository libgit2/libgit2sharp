using System;
using System.Collections;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class ArchiveFixture : BaseFixture
    {
        [Fact]
        public void CanArchiveATree()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var tree = repo.Lookup<Tree>("581f9824ecaf824221bd36edf5430f2739a7c4f5");

                var archiver = new MockArchiver();

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                repo.ObjectDatabase.Archive(tree, archiver);

                var expected = new ArrayList
                {
                    new { Path = "1", Sha = "7f76480d939dc401415927ea7ef25c676b8ddb8f" },
                    new { Path = Path.Combine("1", "branch_file.txt"), Sha = "45b983be36b73c0788dc9cbcb76cbb80fc7bb057" },
                    new { Path = "README", Sha = "a8233120f6ad708f843d861ce2b7228ec4e3dec6" },
                    new { Path = "branch_file.txt", Sha = "45b983be36b73c0788dc9cbcb76cbb80fc7bb057" },
                    new { Path = "new.txt", Sha = "a71586c1dfe8a71c6cbf6c129f404c5642ff31bd" },
                };
                Assert.Equal(expected, archiver.Files);
                Assert.Null(archiver.ReceivedCommitSha);
                Assert.InRange(archiver.ModificationTime, before, DateTimeOffset.UtcNow);
            }
        }

        [Fact]
        public void CanArchiveACommit()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var commit = repo.Lookup<Commit>("4c062a6361ae6959e06292c1fa5e2822d9c96345");

                var archiver = new MockArchiver();

                repo.ObjectDatabase.Archive(commit, archiver);

                var expected = new ArrayList
                {
                    new { Path = "1", Sha = "7f76480d939dc401415927ea7ef25c676b8ddb8f" },
                    new { Path = Path.Combine("1", "branch_file.txt"), Sha = "45b983be36b73c0788dc9cbcb76cbb80fc7bb057" },
                    new { Path = "README", Sha = "a8233120f6ad708f843d861ce2b7228ec4e3dec6" },
                    new { Path = "branch_file.txt", Sha = "45b983be36b73c0788dc9cbcb76cbb80fc7bb057" },
                    new { Path = "new.txt", Sha = "a71586c1dfe8a71c6cbf6c129f404c5642ff31bd" },
                };
                Assert.Equal(expected, archiver.Files);
                Assert.Equal(commit.Sha, archiver.ReceivedCommitSha);
                Assert.Equal(commit.Committer.When, archiver.ModificationTime);
            }
        }

        [Fact]
        public void ArchivingANullTreeOrCommitThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.ObjectDatabase.Archive(default(Commit), default(ArchiverBase)));
                Assert.Throws<ArgumentNullException>(() => repo.ObjectDatabase.Archive(default(Commit), default(string)));
                Assert.Throws<ArgumentNullException>(() => repo.ObjectDatabase.Archive(default(Tree), default(ArchiverBase)));
                Assert.Throws<ArgumentNullException>(() => repo.ObjectDatabase.Archive(default(Tree), default(string)));
            }
        }

        #region MockArchiver

        private class MockArchiver : ArchiverBase
        {
            public readonly ArrayList Files = new ArrayList();
            public string ReceivedCommitSha;
            public DateTimeOffset ModificationTime;

            #region Overrides of ArchiverBase

            public override void BeforeArchiving(Tree tree, ObjectId oid, DateTimeOffset modificationTime)
            {
                if (oid != null)
                {
                    ReceivedCommitSha = oid.Sha;
                }
                ModificationTime = modificationTime;
            }

            protected override void AddTreeEntry(string path, TreeEntry entry, DateTimeOffset modificationTime)
            {
                Files.Add(new { Path = path, entry.Target.Sha });
            }

            #endregion
        }

        #endregion
    }
}
