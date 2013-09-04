using System;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class PushFixture : BaseFixture
    {
        private void OnPushStatusError(PushStatusError pushStatusErrors)
        {
            Assert.True(false, string.Format("Failed to update reference '{0}': {1}",
                pushStatusErrors.Reference, pushStatusErrors.Message));
        }

        private void AssertPush(Action<Repository> push)
        {
            var scd = BuildSelfCleaningDirectory();

            string originalRepoPath = CloneBareTestRepo();
            string clonedRepoPath = Repository.Clone(originalRepoPath, scd.DirectoryPath);

            using (var originalRepo = new Repository(originalRepoPath))
            using (var clonedRepo = new Repository(clonedRepoPath))
            {
                Remote remote = clonedRepo.Network.Remotes["origin"];

                // Compare before
                Assert.Equal(originalRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier,
                             clonedRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier);
                Assert.Equal(
                    clonedRepo.Network.ListReferences(remote).Single(r => r.CanonicalName == "refs/heads/master"),
                    clonedRepo.Refs.Head.ResolveToDirectReference());

                // Change local state (commit)
                const string relativeFilepath = "new_file.txt";
                Touch(clonedRepo.Info.WorkingDirectory, relativeFilepath, "__content__");
                clonedRepo.Index.Stage(relativeFilepath);
                clonedRepo.Commit("__commit_message__", Constants.Signature, Constants.Signature);

                // Assert local state has changed
                Assert.NotEqual(originalRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier,
                                clonedRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier);
                Assert.NotEqual(
                    clonedRepo.Network.ListReferences(remote).Single(r => r.CanonicalName == "refs/heads/master"),
                    clonedRepo.Refs.Head.ResolveToDirectReference());

                // Push the change upstream (remote state is supposed to change)
                push(clonedRepo);

                // Assert that both local and remote repos are in sync
                Assert.Equal(originalRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier,
                             clonedRepo.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier);
                Assert.Equal(
                    clonedRepo.Network.ListReferences(remote).Single(r => r.CanonicalName == "refs/heads/master"),
                    clonedRepo.Refs.Head.ResolveToDirectReference());
            }
        }

        [Fact]
        public void CanPushABranchTrackingAnUpstreamBranch()
        {
            AssertPush(repo => repo.Network.Push(repo.Head));
            AssertPush(repo => repo.Network.Push(repo.Branches["master"]));
            AssertPush(repo => repo.Network.Push(repo.Network.Remotes["origin"], "HEAD", @"refs/heads/master", OnPushStatusError));
        }

        [Fact]
        public void PushingABranchThatDoesNotTrackAnUpstreamBranchThrows()
        {
            Assert.Throws<LibGit2SharpException>(
                () =>
                AssertPush(repo =>
                    {
                        Branch branch = repo.Branches["master"];
                        repo.Branches.Update(branch, b => b.TrackedBranch = null);
                        repo.Network.Push(branch);
                    }));
        }

        [Fact]
        public void CanPushToALocalBareRepository()
        {
            String path = InitNewRepository();

            // Initial repo
            using (var repo = new Repository(path))
            {
                Touch(repo.Info.WorkingDirectory,
                    "test.txt",
                    "This is a test document which will be committed.");

                repo.Index.Stage("test.txt");
                repo.Commit("Test commit.", Constants.Signature, Constants.Signature);
                Assert.NotNull(repo.Head.Tip);
            }

            // Cloning into a bare repository
            // This will be behave like a server-side repository
            SelfCleaningDirectory scd1 = BuildSelfCleaningDirectory();
            string bareClonedPath = Repository.Clone(path, scd1.DirectoryPath, true);

            // Cloning again into a standard repository
            // This will be behave like a standard client repository
            SelfCleaningDirectory scd2 = BuildSelfCleaningDirectory();
            string standardClonedPath = Repository.Clone(bareClonedPath, scd2.DirectoryPath);

            using (var repo = new Repository(standardClonedPath))
            {
                var branch = repo.Branches.Add("otherBranch", repo.Head.Tip);
                repo.Branches.Update(branch, b => b.TrackedBranch = "refs/remotes/origin/otherBranch");

                repo.Branches["otherBranch"].Checkout();

                Assert.DoesNotThrow(() => repo.Network.Push(repo.Head, OnPushStatusError));
            }
        }
    }
}
