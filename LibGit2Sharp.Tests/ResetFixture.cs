using System;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class ResetFixture : BaseFixture
    {
        [TestCase(true)]
        [TestCase(false)]
        public void ResetANewlyInitializedRepositoryThrows(bool isBare)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            string dir = Repository.Init(scd.DirectoryPath, isBare);

            using (var repo = new Repository(dir))
            {
                Assert.Throws<LibGit2Exception>(() => repo.Reset(ResetOptions.Soft, repo.Head.CanonicalName));
            }
        }

        [Test]
        public void SoftResetToTheHeadOfARepositoryDoesNotChangeTheTargetOfTheHead()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Branch oldHead = repo.Head;

                repo.Reset(ResetOptions.Soft, oldHead.CanonicalName);

                repo.Head.ShouldEqual(oldHead);
            }
        }

        [Test]
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

        [Test]
        public void ResettingWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Reset(ResetOptions.Soft, null));
                Assert.Throws<ArgumentException>(() => repo.Reset(ResetOptions.Soft, ""));
                Assert.Throws<LibGit2Exception>(() => repo.Reset(ResetOptions.Soft, Constants.UnknownSha));
            }
        }

        [Test]
        public void SoftResetSetsTheHeadToTheSpecifiedCommit()
        {
            /* Make the Head point to a branch through its name */
            AssertSoftReset(b => b.Name, false, b => b.Name);
        }

        [Test]
        public void SoftResetSetsTheDetachedHeadToTheSpecifiedCommit()
        {
            /* Make the Head point to a commit through its sha (Detaches the Head) */
            AssertSoftReset(b => b.Tip.Sha, true, b => "(no branch)");
        }

        private void AssertSoftReset(Func<Branch, string> branchIdentifierRetriever, bool shouldHeadBeDetached, Func<Branch, string> expectedHeadNameRetriever)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            string dir = Repository.Init(scd.DirectoryPath);

            using (var repo = new Repository(dir))
            {
                FeedTheRepository(repo);

                Tag tag = repo.Tags["mytag"];
                Branch branch = repo.Branches["mybranch"];

                string branchIdentifier = branchIdentifierRetriever(branch);
                repo.Branches.Checkout(branchIdentifier);
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

            repo.Branches.Checkout("mybranch");

            repo.Index.RetrieveStatus().IsDirty.ShouldBeFalse();
        }
    }
}
