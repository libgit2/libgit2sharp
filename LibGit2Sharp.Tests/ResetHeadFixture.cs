﻿using System;
using System.IO;
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
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath, isBare))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Reset(ResetOptions.Soft));
            }
        }

        [Fact]
        public void SoftResetToTheHeadOfARepositoryDoesNotChangeTheTargetOfTheHead()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                IBranch oldHead = repo.Head;

                repo.Reset(ResetOptions.Soft);

                Assert.Equal(oldHead, repo.Head);
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
                Assert.Equal("e90810b8df3e80c413d903f631643c716887138d", repo.Head.Tip.Sha);
            }
        }

        [Fact]
        public void ResettingWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Reset(ResetOptions.Soft, null));
                Assert.Throws<ArgumentException>(() => repo.Reset(ResetOptions.Soft, ""));
                Assert.Throws<LibGit2SharpException>(() => repo.Reset(ResetOptions.Soft, Constants.UnknownSha));
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

        private void AssertSoftReset(Func<IBranch, string> branchIdentifierRetriever, bool shouldHeadBeDetached, Func<IBranch, string> expectedHeadNameRetriever)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                FeedTheRepository(repo);

                Tag tag = repo.Tags["mytag"];
                IBranch branch = repo.Branches["mybranch"];

                string branchIdentifier = branchIdentifierRetriever(branch);
                repo.Checkout(branchIdentifier);
                Assert.Equal(shouldHeadBeDetached, repo.Info.IsHeadDetached);

                string expectedHeadName = expectedHeadNameRetriever(branch);
                Assert.Equal(expectedHeadName, repo.Head.Name);
                Assert.Equal(branch.Tip.Sha, repo.Head.Tip.Sha);

                /* Reset --soft the Head to a tag through its canonical name */
                repo.Reset(ResetOptions.Soft, tag.CanonicalName);
                Assert.Equal(expectedHeadName, repo.Head.Name);
                Assert.Equal(tag.Target.Id, repo.Head.Tip.Id);

                Assert.Equal(FileStatus.Staged, repo.Index.RetrieveStatus("a.txt"));

                /* Reset --soft the Head to a commit through its sha */
                repo.Reset(ResetOptions.Soft, branch.Tip.Sha);
                Assert.Equal(expectedHeadName, repo.Head.Name);
                Assert.Equal(branch.Tip.Sha, repo.Head.Tip.Sha);

                Assert.Equal(FileStatus.Unaltered, repo.Index.RetrieveStatus("a.txt"));
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

                Assert.Equal(FileStatus.Modified, repo.Index.RetrieveStatus("a.txt"));
            }
        }

        [Fact]
        public void MixedResetInABareRepositoryThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Reset(ResetOptions.Mixed));
            }
        }
    }
}
