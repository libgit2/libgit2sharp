﻿using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class MergeFixture : BaseFixture
    {
        [Fact]
        public void ANewRepoIsFullyMerged()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Assert.Equal(true, repo.Index.IsFullyMerged);
                Assert.Empty(repo.MergeHeads);
            }
        }

        [Fact]
        public void AFullyMergedRepoOnlyContainsStagedIndexEntries()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                Assert.Equal(true, repo.Index.IsFullyMerged);
                Assert.Empty(repo.MergeHeads);

                foreach (var entry in repo.Index)
                {
                    Assert.Equal(StageLevel.Staged, entry.StageLevel);
                }
            }
        }

        [Fact]
        public void SoftResetARepoWithUnmergedEntriesThrows()
        {
            using (var repo = new Repository(MergedTestRepoWorkingDirPath))
            {
                Assert.Equal(false, repo.Index.IsFullyMerged);

                var headCommit = repo.Head.Tip;
                var firstCommitParent = headCommit.Parents.First();
                Assert.Throws<UnmergedIndexEntriesException>(
                    () => repo.Reset(ResetMode.Soft, firstCommitParent));
            }
        }

        [Fact]
        public void CommitAgainARepoWithUnmergedEntriesThrows()
        {
            using (var repo = new Repository(MergedTestRepoWorkingDirPath))
            {
                Assert.Equal(false, repo.Index.IsFullyMerged);

                var author = Constants.Signature;
                Assert.Throws<UnmergedIndexEntriesException>(
                    () => repo.Commit("Try commit unmerged entries", author, author));
            }
        }

        [Fact]
        public void CanRetrieveTheBranchBeingMerged()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string firstBranch = "9fd738e8f7967c078dceed8190330fc8648ee56a";
                const string secondBranch = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef";

                Touch(repo.Info.Path, "MERGE_HEAD", string.Format("{0}{1}{2}{1}", firstBranch, "\n", secondBranch));

                Assert.Equal(CurrentOperation.Merge, repo.Info.CurrentOperation);

                MergeHead[] mergedHeads = repo.MergeHeads.ToArray();
                Assert.Equal("MERGE_HEAD[0]", mergedHeads[0].Name);
                Assert.Equal(firstBranch, mergedHeads[0].Tip.Id.Sha);
                Assert.Equal("MERGE_HEAD[1]", mergedHeads[1].Name);
                Assert.Null(mergedHeads[1].Tip);
            }
        }
    }
}
