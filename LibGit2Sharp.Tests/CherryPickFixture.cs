using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class CherryPickFixture : BaseFixture
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanCherryPick(bool fromDetachedHead)
        {
            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                if (fromDetachedHead)
                {
                    repo.Checkout(repo.Head.Tip.Id.Sha);
                }

                Commit commitToMerge = repo.Branches["fast_forward"].Tip;

                CherryPickResult result = repo.CherryPick(commitToMerge, Constants.Signature);

                Assert.Equal(CherryPickStatus.CherryPicked, result.Status);
                Assert.Equal(cherryPickedCommitId, result.Commit.Id.Sha);
                Assert.False(repo.RetrieveStatus().Any());
                Assert.Equal(fromDetachedHead, repo.Info.IsHeadDetached);
                Assert.Equal(commitToMerge.Author, result.Commit.Author);
                Assert.Equal(Constants.Signature, result.Commit.Committer);
            }
        }

        [Fact]
        public void CherryPickWithConflictDoesNotCommit()
        {
            const string firstBranchFileName = "first branch file.txt";
            const string secondBranchFileName = "second branch file.txt";
            const string sharedBranchFileName = "first+second branch file.txt";

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var firstBranch = repo.CreateBranch("FirstBranch");
                repo.Checkout(firstBranch);

                // Commit with ONE new file to both first & second branch (SecondBranch is created on this commit).
                AddFileCommitToRepo(repo, sharedBranchFileName);

                var secondBranch = repo.CreateBranch("SecondBranch");
                // Commit with ONE new file to first branch (FirstBranch moves forward as it is checked out, SecondBranch stays back one).
                AddFileCommitToRepo(repo, firstBranchFileName);
                AddFileCommitToRepo(repo, sharedBranchFileName, "The first branches comment");  // Change file in first branch

                repo.Checkout(secondBranch);
                // Commit with ONE new file to second branch (FirstBranch and SecondBranch now point to separate commits that both have the same parent commit).
                AddFileCommitToRepo(repo, secondBranchFileName);
                AddFileCommitToRepo(repo, sharedBranchFileName, "The second branches comment");  // Change file in second branch

                CherryPickResult cherryPickResult = repo.CherryPick(repo.Branches["FirstBranch"].Tip, Constants.Signature);

                Assert.Equal(CherryPickStatus.Conflicts, cherryPickResult.Status);

                Assert.Null(cherryPickResult.Commit);
                Assert.Equal(1, repo.Index.Conflicts.Count());

                var conflict = repo.Index.Conflicts.First();
                var changes = repo.Diff.Compare(repo.Lookup<Blob>(conflict.Theirs.Id), repo.Lookup<Blob>(conflict.Ours.Id));

                Assert.False(changes.IsBinaryComparison);
            }
        }

        [Theory]
        [InlineData(CheckoutFileConflictStrategy.Ours)]
        [InlineData(CheckoutFileConflictStrategy.Theirs)]
        public void CanSpecifyConflictFileStrategy(CheckoutFileConflictStrategy conflictStrategy)
        {
            const string conflictFile = "a.txt";
            const string conflictBranchName = "conflicts";

            string path = SandboxMergeTestRepo();
            using (var repo = new Repository(path))
            {
                Branch branch = repo.Branches[conflictBranchName];
                Assert.NotNull(branch);

                CherryPickOptions cherryPickOptions = new CherryPickOptions()
                {
                    FileConflictStrategy = conflictStrategy,
                };

                CherryPickResult result = repo.CherryPick(branch.Tip, Constants.Signature, cherryPickOptions);
                Assert.Equal(CherryPickStatus.Conflicts, result.Status);

                // Get the information on the conflict.
                Conflict conflict = repo.Index.Conflicts[conflictFile];

                Assert.NotNull(conflict);
                Assert.NotNull(conflict.Theirs);
                Assert.NotNull(conflict.Ours);

                // Get the blob containing the expected content.
                Blob expectedBlob = null;
                switch (conflictStrategy)
                {
                    case CheckoutFileConflictStrategy.Theirs:
                        expectedBlob = repo.Lookup<Blob>(conflict.Theirs.Id);
                        break;
                    case CheckoutFileConflictStrategy.Ours:
                        expectedBlob = repo.Lookup<Blob>(conflict.Ours.Id);
                        break;
                    default:
                        throw new Exception("Unexpected FileConflictStrategy");
                }

                Assert.NotNull(expectedBlob);

                // Check the content of the file on disk matches what is expected.
                string expectedContent = expectedBlob.GetContentText(new FilteringOptions(conflictFile));
                Assert.Equal(expectedContent, File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, conflictFile)));
            }
        }

        private Commit AddFileCommitToRepo(IRepository repository, string filename, string content = null)
        {
            Touch(repository.Info.WorkingDirectory, filename, content);

            repository.Stage(filename);

            return repository.Commit("New commit", Constants.Signature, Constants.Signature);
        }

        // Commit IDs of the checked in merge_testrepo
        private const string cherryPickedCommitId = "74b37f366b6e1c682c1c9fe0c6b006cbe909cf91";
    }
}
