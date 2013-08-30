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
                Assert.Throws<LibGit2SharpException>(() => repo.Reset(ResetOptions.Soft));
            }
        }

        [Fact]
        public void SoftResetToTheHeadOfARepositoryDoesNotChangeTheTargetOfTheHead()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch oldHead = repo.Head;

                repo.Reset(ResetOptions.Soft);

                Assert.Equal(oldHead, repo.Head);
            }
        }

        [Fact]
        public void SoftResetToAParentCommitChangesTheTargetOfTheHead()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                var headCommit = repo.Head.Tip;
                var firstCommitParent = headCommit.Parents.First();
                repo.Reset(ResetOptions.Soft, firstCommitParent);

                Assert.Equal(firstCommitParent, repo.Head.Tip);
            }
        }

        [Fact]
        public void SoftResetSetsTheHeadToTheDereferencedCommitOfAChainedTag()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                Tag tag = repo.Tags["test"];
                repo.Reset(ResetOptions.Soft, tag.CanonicalName);
                Assert.Equal("e90810b8df3e80c413d903f631643c716887138d", repo.Head.Tip.Sha);
            }
        }

        [Fact]
        public void ResettingWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Reset(ResetOptions.Soft, (string)null));
                Assert.Throws<ArgumentNullException>(() => repo.Reset(ResetOptions.Soft, (Commit)null));
                Assert.Throws<ArgumentException>(() => repo.Reset(ResetOptions.Soft, ""));
                Assert.Throws<LibGit2SharpException>(() => repo.Reset(ResetOptions.Soft, Constants.UnknownSha));
                Assert.Throws<LibGit2SharpException>(() => repo.Reset(ResetOptions.Soft, repo.Head.Tip.Tree.Sha));
            }
        }

        [Fact]
        public void SoftResetSetsTheHeadToTheSpecifiedCommit()
        {
            /* Make the Head point to a branch through its name */
            AssertSoftReset(b => b.Name, false, b => b.Name);
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

            using (var repo = new Repository(repoPath))
            {
                FeedTheRepository(repo);

                Tag tag = repo.Tags["mytag"];
                Branch branch = repo.Branches["mybranch"];

                string branchIdentifier = branchIdentifierRetriever(branch);
                repo.Checkout(branchIdentifier);
                var oldHeadId = repo.Head.Tip.Id;
                Assert.Equal(shouldHeadBeDetached, repo.Info.IsHeadDetached);

                string expectedHeadName = expectedHeadNameRetriever(branch);
                Assert.Equal(expectedHeadName, repo.Head.Name);
                Assert.Equal(branch.Tip.Sha, repo.Head.Tip.Sha);

                /* Reset --soft the Head to a tag through its canonical name */
                repo.Reset(ResetOptions.Soft, tag.CanonicalName);
                Assert.Equal(expectedHeadName, repo.Head.Name);
                Assert.Equal(tag.Target.Id, repo.Head.Tip.Id);

                Assert.Equal(FileStatus.Staged, repo.Index.RetrieveStatus("a.txt"));

                AssertRefLogEntry(repo, "HEAD",
                                  tag.Target.Id,
                                  string.Format("reset: moving to {0}", tag.Target.Sha),
                                  oldHeadId);

                /* Reset --soft the Head to a commit through its sha */
                repo.Reset(ResetOptions.Soft, branch.Tip.Sha);
                Assert.Equal(expectedHeadName, repo.Head.Name);
                Assert.Equal(branch.Tip.Sha, repo.Head.Tip.Sha);

                Assert.Equal(FileStatus.Unaltered, repo.Index.RetrieveStatus("a.txt"));

                AssertRefLogEntry(repo, "HEAD",
                                  branch.Tip.Id,
                                  string.Format("reset: moving to {0}", branch.Tip.Sha),
                                  tag.Target.Id);
            }
        }

        private static void FeedTheRepository(Repository repo)
        {
            string fullPath = Touch(repo.Info.WorkingDirectory, "a.txt", "Hello\n");
            repo.Index.Stage(fullPath);
            repo.Commit("Initial commit", Constants.Signature, Constants.Signature);
            repo.ApplyTag("mytag");

            File.AppendAllText(fullPath, "World\n");
            repo.Index.Stage(fullPath);

            Signature shiftedSignature = Constants.Signature.TimeShift(TimeSpan.FromMinutes(1));
            repo.Commit("Update file", shiftedSignature, shiftedSignature);
            repo.CreateBranch("mybranch");

            repo.Checkout("mybranch");

            Assert.False(repo.Index.RetrieveStatus().IsDirty);
        }

        [Fact]
        public void MixedResetRefreshesTheIndex()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                FeedTheRepository(repo);

                var oldHeadId = repo.Head.Tip.Id;

                Tag tag = repo.Tags["mytag"];

                repo.Reset(ResetOptions.Mixed, tag.CanonicalName);

                Assert.Equal(FileStatus.Modified, repo.Index.RetrieveStatus("a.txt"));

                AssertRefLogEntry(repo, "HEAD",
                                  tag.Target.Id,
                                  string.Format("reset: moving to {0}", tag.Target.Sha),
                                  oldHeadId);
            }
        }

        [Fact]
        public void MixedResetInABareRepositoryThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<BareRepositoryException>(() => repo.Reset(ResetOptions.Mixed));
            }
        }

        [Fact]
        public void HardResetInABareRepositoryThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<BareRepositoryException>(() => repo.Reset(ResetOptions.Hard));
            }
        }

        [Fact]
        public void HardResetUpdatesTheContentOfTheWorkingDirectory()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var names = new DirectoryInfo(repo.Info.WorkingDirectory).GetFileSystemInfos().Select(fsi => fsi.Name).ToList();

                File.Delete(Path.Combine(repo.Info.WorkingDirectory, "README"));
                Touch(repo.Info.WorkingDirectory, "WillNotBeRemoved.txt", "content\n");

                Assert.True(names.Count > 4);

                repo.Reset(ResetOptions.Hard, "HEAD~3");

                names = new DirectoryInfo(repo.Info.WorkingDirectory).GetFileSystemInfos().Select(fsi => fsi.Name).ToList();
                names.Sort(StringComparer.Ordinal);

                Assert.Equal(new[] { ".git", "README", "WillNotBeRemoved.txt", "branch_file.txt", "new.txt", "new_untracked_file.txt" }, names);
            }
        }
    }
}
