using System;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class ResetFixture : BaseFixture
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ResetANewlyInitializedRepositoryThrows(bool isBare)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath, isBare))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Reset(ResetOptions.Soft, repo.Head.CanonicalName));
            }
        }

        [Fact]
        public void SoftResetToTheHeadOfARepositoryDoesNotChangeTheTargetOfTheHead()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Branch oldHead = repo.Head;

                repo.Reset(ResetOptions.Soft, oldHead.CanonicalName);

                repo.Head.ShouldEqual(oldHead);
            }
        }

        [Fact]
        public void SoftResetSetsTheHeadToTheDereferencedCommitOfAChainedTag()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo();

            using (var repo = new Repository(path.RepositoryPath))
            {
                Tag tag = repo.Tags["test"];
                repo.Reset(ResetOptions.Soft, tag.CanonicalName);
                repo.Head.Tip.Sha.ShouldEqual("e90810b8df3e80c413d903f631643c716887138d");
            }
        }

        [Fact]
        public void ResettingWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Reset(ResetOptions.Soft, null));
                Assert.Throws<ArgumentException>(() => repo.Reset(ResetOptions.Soft, ""));
                Assert.Throws<LibGit2Exception>(() => repo.Reset(ResetOptions.Soft, Constants.UnknownSha));
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
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath)) 
            {
                FeedTheRepository(repo);

                Tag tag = repo.Tags["mytag"];
                Branch branch = repo.Branches["mybranch"];

                string branchIdentifier = branchIdentifierRetriever(branch);
                repo.Checkout(branchIdentifier);
                repo.Info.IsHeadDetached.ShouldEqual(shouldHeadBeDetached);

                string expectedHeadName = expectedHeadNameRetriever(branch);
                repo.Head.Name.ShouldEqual(expectedHeadName);
                repo.Head.Tip.Sha.ShouldEqual(branch.Tip.Sha);

                /* Reset --soft the Head to a tag through its canonical name */
                repo.Reset(ResetOptions.Soft, tag.CanonicalName);
                repo.Head.Name.ShouldEqual(expectedHeadName);
                repo.Head.Tip.Id.ShouldEqual(tag.Target.Id);

                repo.Index.RetrieveStatus("a.txt").ShouldEqual(FileStatus.Staged);

                /* Reset --soft the Head to a commit through its sha */
                repo.Reset(ResetOptions.Soft, branch.Tip.Sha);
                repo.Head.Name.ShouldEqual(expectedHeadName);
                repo.Head.Tip.Sha.ShouldEqual(branch.Tip.Sha);

                repo.Index.RetrieveStatus("a.txt").ShouldEqual(FileStatus.Unaltered);
            }
        }

        private static void FeedTheRepository(Repository repo)
        {
            string fullPath = Path.Combine(repo.Info.WorkingDirectory, "a.txt");
            File.WriteAllText(fullPath, "Hello\n");
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
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                FeedTheRepository(repo);

                Tag tag = repo.Tags["mytag"];

                repo.Reset(ResetOptions.Mixed, tag.CanonicalName);

                repo.Index.RetrieveStatus("a.txt").ShouldEqual(FileStatus.Modified);
            }
        }

        [Fact]
        public void MixedResetInABareRepositoryThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Reset(ResetOptions.Mixed, repo.Head.Tip.Sha));
            }
        }
    }
}
