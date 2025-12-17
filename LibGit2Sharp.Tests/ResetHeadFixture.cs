using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class ResetHeadFixture : BaseFixture
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ResetANewlyInitializedRepositoryThrows(bool isBare)
        {
            string repoPath = InitNewRepository(isBare);

            using (var repo = new Repository(repoPath))
            {
                Assert.Throws<UnbornBranchException>(() => repo.Reset(ResetMode.Soft));
            }
        }

        [Fact]
        public void SoftResetToTheHeadOfARepositoryDoesNotChangeTheTargetOfTheHead()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch oldHead = repo.Head;

                repo.Reset(ResetMode.Soft);

                Assert.Equal(oldHead, repo.Head);
            }
        }

        [Fact]
        public void SoftResetToAParentCommitChangesTheTargetOfTheHead()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var headCommit = repo.Head.Tip;
                var firstCommitParent = headCommit.Parents.First();
                repo.Reset(ResetMode.Soft, firstCommitParent);

                Assert.Equal(firstCommitParent, repo.Head.Tip);
            }
        }

        [Fact]
        public void SoftResetSetsTheHeadToTheDereferencedCommitOfAChainedTag()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag tag = repo.Tags["test"];
                repo.Reset(ResetMode.Soft, tag.CanonicalName);
                Assert.Equal("e90810b8df3e80c413d903f631643c716887138d", repo.Head.Tip.Sha);
            }
        }

        [Fact]
        public void ResettingWithBadParamsThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Reset(ResetMode.Soft, (string)null));
                Assert.Throws<ArgumentNullException>(() => repo.Reset(ResetMode.Soft, (Commit)null));
                Assert.Throws<ArgumentException>(() => repo.Reset(ResetMode.Soft, ""));
                Assert.Throws<NotFoundException>(() => repo.Reset(ResetMode.Soft, Constants.UnknownSha));
                Assert.Throws<InvalidSpecificationException>(() => repo.Reset(ResetMode.Soft, repo.Head.Tip.Tree.Sha));
            }
        }

        [Fact]
        public void SoftResetSetsTheHeadToTheSpecifiedCommit()
        {
            /* Make the Head point to a branch through its name */
            AssertSoftReset(b => b.FriendlyName, false, b => b.FriendlyName);
        }

        [Fact]
        public void SoftResetSetsTheDetachedHeadToTheSpecifiedCommit()
        {
            /* Make the Head point to a commit through its sha (Detaches the Head) */
            AssertSoftReset(b => b.Tip.Sha, true, b => "(no branch)");
        }

        private void AssertSoftReset(Func<Branch, string> branchIdentifierRetriever, bool shouldHeadBeDetached, Func<Branch, string> expectedHeadNameRetriever)
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath, new RepositoryOptions { Identity = Constants.Identity }))
            {
                FeedTheRepository(repo);

                Tag tag = repo.Tags["mytag"];
                Branch branch = repo.Branches["mybranch"];

                string branchIdentifier = branchIdentifierRetriever(branch);
                Commands.Checkout(repo, branchIdentifier);
                var oldHeadId = repo.Head.Tip.Id;
                Assert.Equal(shouldHeadBeDetached, repo.Info.IsHeadDetached);

                string expectedHeadName = expectedHeadNameRetriever(branch);
                Assert.Equal(expectedHeadName, repo.Head.FriendlyName);
                Assert.Equal(branch.Tip.Sha, repo.Head.Tip.Sha);

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                /* Reset --soft the Head to a tag through its canonical name */
                repo.Reset(ResetMode.Soft, tag.CanonicalName);
                Assert.Equal(expectedHeadName, repo.Head.FriendlyName);
                Assert.Equal(tag.Target.Id, repo.Head.Tip.Id);

                Assert.Equal(FileStatus.ModifiedInIndex, repo.RetrieveStatus("a.txt"));

                AssertRefLogEntry(repo, "HEAD",
                                  string.Format("reset: moving to {0}", tag.Target.Sha),
                                  oldHeadId,
                                  tag.Target.Id,
                                  Constants.Identity, before);

                if (!shouldHeadBeDetached)
                {
                    AssertRefLogEntry(repo, branch.CanonicalName,
                                      string.Format("reset: moving to {0}", tag.Target.Sha),
                                      oldHeadId,
                                      tag.Target.Id,
                                      Constants.Identity, before);
                }

                before = DateTimeOffset.Now.TruncateMilliseconds();

                /* Reset --soft the Head to a commit through its sha */
                repo.Reset(ResetMode.Soft, branch.Tip.Sha);
                Assert.Equal(expectedHeadName, repo.Head.FriendlyName);
                Assert.Equal(branch.Tip.Sha, repo.Head.Tip.Sha);

                Assert.Equal(FileStatus.Unaltered, repo.RetrieveStatus("a.txt"));

                AssertRefLogEntry(repo, "HEAD",
                                  string.Format("reset: moving to {0}", branch.Tip.Sha),
                                  tag.Target.Id,
                                  branch.Tip.Id,
                                  Constants.Identity, before);

                if (!shouldHeadBeDetached)
                {
                    AssertRefLogEntry(repo, branch.CanonicalName,
                                  string.Format("reset: moving to {0}", branch.Tip.Sha),
                                  tag.Target.Id,
                                  branch.Tip.Id,
                                  Constants.Identity, before);
                }
            }
        }

        private static void FeedTheRepository(IRepository repo)
        {
            string fullPath = Touch(repo.Info.WorkingDirectory, "a.txt", "Hello\n");
            Commands.Stage(repo, fullPath);
            repo.Commit("Initial commit", Constants.Signature, Constants.Signature);
            repo.ApplyTag("mytag");

            File.AppendAllText(fullPath, "World\n");
            Commands.Stage(repo, fullPath);

            Signature shiftedSignature = Constants.Signature.TimeShift(TimeSpan.FromMinutes(1));
            repo.Commit("Update file", shiftedSignature, shiftedSignature);
            repo.CreateBranch("mybranch");

            Commands.Checkout(repo, "mybranch");

            Assert.False(repo.RetrieveStatus().IsDirty);
        }

        [Fact]
        public void MixedResetRefreshesTheIndex()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath, new RepositoryOptions { Identity = Constants.Identity }))
            {
                FeedTheRepository(repo);

                var oldHeadId = repo.Head.Tip.Id;

                Tag tag = repo.Tags["mytag"];

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                repo.Reset(ResetMode.Mixed, tag.CanonicalName);

                Assert.Equal(FileStatus.ModifiedInWorkdir, repo.RetrieveStatus("a.txt"));

                AssertRefLogEntry(repo, "HEAD",
                                  string.Format("reset: moving to {0}", tag.Target.Sha),
                                  oldHeadId,
                                  tag.Target.Id,
                                  Constants.Identity, before);

                AssertRefLogEntry(repo, "refs/heads/mybranch",
                                  string.Format("reset: moving to {0}", tag.Target.Sha),
                                  oldHeadId,
                                  tag.Target.Id,
                                  Constants.Identity, before);
            }
        }

        [Fact]
        public void MixedResetInABareRepositoryThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<BareRepositoryException>(() => repo.Reset(ResetMode.Mixed));
            }
        }

        [Fact]
        public void HardResetInABareRepositoryThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<BareRepositoryException>(() => repo.Reset(ResetMode.Hard));
            }
        }

        [Fact]
        public void HardResetUpdatesTheContentOfTheWorkingDirectory()
        {
            bool progressCalled = false;

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var names = new DirectoryInfo(repo.Info.WorkingDirectory).GetFileSystemInfos().Select(fsi => fsi.Name).ToList();

                File.Delete(Path.Combine(repo.Info.WorkingDirectory, "README"));
                Touch(repo.Info.WorkingDirectory, "WillNotBeRemoved.txt", "content\n");

                Assert.True(names.Count > 4);

                var commit = repo.Lookup<Commit>("HEAD~3");
                repo.Reset(ResetMode.Hard, commit, new CheckoutOptions()
                {
                    OnCheckoutProgress = (_path, _completed, _total) => { progressCalled = true; },
                });

                names = new DirectoryInfo(repo.Info.WorkingDirectory).GetFileSystemInfos().Select(fsi => fsi.Name).ToList();
                names.Sort(StringComparer.Ordinal);

                Assert.True(progressCalled);
                Assert.Equal(new[] { ".git", "README", "WillNotBeRemoved.txt", "branch_file.txt", "new.txt", "new_untracked_file.txt" }, names);
            }
        }
    }
}
