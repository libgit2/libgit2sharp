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

        private void AssertPush(Action<IRepository> push)
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
            bool packBuilderCalled = false;
            Handlers.PackBuilderProgressHandler packBuilderCb = (x, y, z) => { packBuilderCalled = true; return true; };

            AssertPush(repo => repo.Network.Push(repo.Head));
            AssertPush(repo => repo.Network.Push(repo.Branches["master"]));

            PushOptions options = new PushOptions()
            {
                OnPushStatusError = OnPushStatusError,
                OnPackBuilderProgress = packBuilderCb,
            };

            AssertPush(repo => repo.Network.Push(repo.Network.Remotes["origin"], "HEAD", @"refs/heads/master", options));
            Assert.True(packBuilderCalled);
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
        public void CanForcePush()
        {
            string remoteRepoPath = InitNewRepository(true);

            // Create a new repository
            string localRepoPath = InitNewRepository();
            using (var localRepo = new Repository(localRepoPath))
            {
                // Add a commit
                Commit first = AddCommitToRepo(localRepo);

                Remote remote = localRepo.Network.Remotes.Add("origin", remoteRepoPath);

                localRepo.Branches.Update(localRepo.Head,
                    b => b.Remote = remote.Name,
                    b => b.UpstreamBranch = localRepo.Head.CanonicalName);

                // Push this commit
                localRepo.Network.Push(localRepo.Head);
                AssertRemoteHeadTipEquals(localRepo, first.Sha);

                UpdateTheRemoteRepositoryWithANewCommit(remoteRepoPath);

                // Add another commit
                var oldId = localRepo.Head.Tip.Id;
                Commit second = AddCommitToRepo(localRepo);

                // Try to fast forward push this new commit
                Assert.Throws<NonFastForwardException>(() => localRepo.Network.Push(localRepo.Head));

                // Force push the new commit
                string pushRefSpec = string.Format("+{0}:{0}", localRepo.Head.CanonicalName);
                localRepo.Network.Push(localRepo.Network.Remotes.Single(), pushRefSpec);

                AssertRemoteHeadTipEquals(localRepo, second.Sha);

                AssertRefLogEntry(localRepo, "refs/remotes/origin/master",
                    localRepo.Head.Tip.Id, "update by push",
                    oldId);
            }
        }

        private static void AssertRemoteHeadTipEquals(IRepository localRepo, string sha)
        {
            var remoteReferences = localRepo.Network.ListReferences(localRepo.Network.Remotes.Single());
            DirectReference remoteHead = remoteReferences.Single(r => r.CanonicalName == "HEAD");

            Assert.Equal(sha, remoteHead.TargetIdentifier);
        }

        private void UpdateTheRemoteRepositoryWithANewCommit(string remoteRepoPath)
        {
            // Perform a fresh clone of the upstream repository
            var scd = BuildSelfCleaningDirectory();
            string clonedRepoPath = Repository.Clone(remoteRepoPath, scd.DirectoryPath);

            using (var clonedRepo = new Repository(clonedRepoPath))
            {
                // Add a commit
                AddCommitToRepo(clonedRepo);

                // Push this new commit toward an upstream repository
                clonedRepo.Network.Push(clonedRepo.Head);
            }
        }

        private Commit AddCommitToRepo(IRepository repository)
        {

            string random = Guid.NewGuid().ToString();
            string filename = random + ".txt";

            Touch(repository.Info.WorkingDirectory, filename, random);

            repository.Index.Stage(filename);

            return repository.Commit("New commit", Constants.Signature, Constants.Signature);
        }
    }
}
