using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class NetworkFixture : BaseFixture
    {
        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository.git")]
        public void CanListRemoteReferences(string url)
        {
            string remoteName = "testRemote";

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, url);
                IList<Reference> references = repo.Network.ListReferences(remote).ToList();


                foreach (var reference in references)
                {
                    // None of those references point to an existing
                    // object in this brand new repository
                    Assert.Null(reference.ResolveToDirectReference().Target);
                }

                List<Tuple<string, string>> actualRefs = references.
                    Select(directRef => new Tuple<string, string>(directRef.CanonicalName, directRef.ResolveToDirectReference()
                        .TargetIdentifier)).ToList();

                Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs.Count, actualRefs.Count);
                Assert.True(references.Single(reference => reference.CanonicalName == "HEAD") is SymbolicReference);
                for (int i = 0; i < TestRemoteRefs.ExpectedRemoteRefs.Count; i++)
                {
                    Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs[i].Item2, actualRefs[i].Item2);
                    Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs[i].Item1, actualRefs[i].Item1);
                }
            }
        }

        [Theory]
        [InlineData("http://github.com/libgit2/TestGitRepository")]
        [InlineData("https://github.com/libgit2/TestGitRepository")]
        [InlineData("git://github.com/libgit2/TestGitRepository.git")]
        public void CanListRemoteReferencesFromUrl(string url)
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                IList<Reference> references = repo.Network.ListReferences(url).ToList();

                foreach (var reference in references)
                {
                    // None of those references point to an existing
                    // object in this brand new repository
                    Assert.Null(reference.ResolveToDirectReference().Target);
                }

                List<Tuple<string, string>> actualRefs = references.
                    Select(directRef => new Tuple<string, string>(directRef.CanonicalName, directRef.ResolveToDirectReference()
                        .TargetIdentifier)).ToList();

                Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs.Count, actualRefs.Count);
                Assert.True(references.Single(reference => reference.CanonicalName == "HEAD") is SymbolicReference);
                for (int i = 0; i < TestRemoteRefs.ExpectedRemoteRefs.Count; i++)
                {
                    Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs[i].Item2, actualRefs[i].Item2);
                    Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs[i].Item1, actualRefs[i].Item1);
                }
            }
        }

        [Fact]
        public void CanListRemoteReferenceObjects()
        {
            const string url = "http://github.com/libgit2/TestGitRepository";
            const string remoteName = "origin";

            var scd = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

            using (var repo = new Repository(clonedRepoPath))
            {
                Remote remote = repo.Network.Remotes[remoteName];
                IEnumerable<Reference> references = repo.Network.ListReferences(remote).ToList();

                var actualRefs = new List<Tuple<string,string>>();

                foreach(Reference reference in references)
                {
                    Assert.NotNull(reference.CanonicalName);

                    var directReference = reference.ResolveToDirectReference();

                    Assert.NotNull(directReference.Target);
                    actualRefs.Add(new Tuple<string, string>(reference.CanonicalName, directReference.Target.Id.Sha));
                }

                Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs.Count, actualRefs.Count);
                Assert.True(references.Single(reference => reference.CanonicalName == "HEAD") is SymbolicReference);
                for (int i = 0; i < TestRemoteRefs.ExpectedRemoteRefs.Count; i++)
                {
                    Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs[i].Item1, actualRefs[i].Item1);
                    Assert.Equal(TestRemoteRefs.ExpectedRemoteRefs[i].Item2, actualRefs[i].Item2);
                }
            }
        }

        [SkippableFact]
        public void CanListRemoteReferencesWithCredentials()
        {
            InconclusiveIf(() => string.IsNullOrEmpty(Constants.PrivateRepoUrl),
                "Populate Constants.PrivateRepo* to run this test");

            string remoteName = "origin";

            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                Remote remote = repo.Network.Remotes.Add(remoteName, Constants.PrivateRepoUrl);

                var references = repo.Network.ListReferences(remote, Constants.PrivateRepoCredentials);

                foreach (var reference in references)
                {
                    Assert.NotNull(reference);
                }
            }
        }

        [Theory]
        [InlineData(FastForwardStrategy.Default)]
        [InlineData(FastForwardStrategy.NoFastForward)]
        public void CanPull(FastForwardStrategy fastForwardStrategy)
        {
            string url = "https://github.com/libgit2/TestGitRepository";

            var scd = BuildSelfCleaningDirectory();
            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

            using (var repo = new Repository(clonedRepoPath))
            {
                repo.Reset(ResetMode.Hard, "HEAD~1");

                Assert.False(repo.RetrieveStatus().Any());
                Assert.Equal(repo.Lookup<Commit>("refs/remotes/origin/master~1"), repo.Head.Tip);

                PullOptions pullOptions = new PullOptions()
                {
                    MergeOptions = new MergeOptions()
                    {
                        FastForwardStrategy = fastForwardStrategy
                    }
                };

                MergeResult mergeResult = repo.Network.Pull(Constants.Signature, pullOptions);

                if(fastForwardStrategy == FastForwardStrategy.Default || fastForwardStrategy == FastForwardStrategy.FastForwardOnly)
                {
                    Assert.Equal(mergeResult.Status, MergeStatus.FastForward);
                    Assert.Equal(mergeResult.Commit, repo.Branches["refs/remotes/origin/master"].Tip);
                    Assert.Equal(repo.Head.Tip, repo.Branches["refs/remotes/origin/master"].Tip);
                }
                else
                {
                    Assert.Equal(mergeResult.Status, MergeStatus.NonFastForward);
                }
            }
        }

        [Fact]
        public void CanPullIntoEmptyRepo()
        {
            string url = "https://github.com/libgit2/TestGitRepository";
            string remoteName = "origin";
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                // Set up remote
                Remote remote = repo.Network.Remotes.Add(remoteName, url);

                // Set up tracking information
                repo.Branches.Update(repo.Head,
                    b => b.Remote = remoteName,
                    b => b.UpstreamBranch = "refs/heads/master");

                // Pull!
                MergeResult mergeResult = repo.Network.Pull(Constants.Signature, new PullOptions());

                Assert.Equal(mergeResult.Status, MergeStatus.FastForward);
                Assert.Equal(mergeResult.Commit, repo.Branches["refs/remotes/origin/master"].Tip);
                Assert.Equal(repo.Head.Tip, repo.Branches["refs/remotes/origin/master"].Tip);
            }
        }

        [Fact]
        public void PullWithoutMergeBranchThrows()
        {
            var scd = BuildSelfCleaningDirectory();
            string clonedRepoPath = Repository.Clone(StandardTestRepoPath, scd.DirectoryPath);

            using (var repo = new Repository(clonedRepoPath))
            {
                Branch branch = repo.Branches["master"];

                // Update the Upstream merge branch
                repo.Branches.Update(branch,
                    b => b.UpstreamBranch = "refs/heads/another_master");

                bool didPullThrow = false;
                MergeFetchHeadNotFoundException thrownException = null;

                try
                {
                    repo.Network.Pull(Constants.Signature, new PullOptions());
                }
                catch(MergeFetchHeadNotFoundException ex)
                {
                    didPullThrow = true;
                    thrownException = ex;
                }

                Assert.True(didPullThrow, "Pull did not throw.");
                Assert.True(thrownException.Message.Contains("refs/heads/another_master"), "Exception message did not contain expected reference.");
            }
        }

        [Fact]
        public void CanMergeFetchedRefs()
        {
            string url = "https://github.com/libgit2/TestGitRepository";

            var scd = BuildSelfCleaningDirectory();
            string clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

            using (var repo = new Repository(clonedRepoPath))
            {
                repo.Reset(ResetMode.Hard, "HEAD~1");

                Assert.False(repo.RetrieveStatus().Any());
                Assert.Equal(repo.Lookup<Commit>("refs/remotes/origin/master~1"), repo.Head.Tip);

                repo.Network.Fetch(repo.Head.Remote);

                MergeOptions mergeOptions = new MergeOptions()
                {
                    FastForwardStrategy = FastForwardStrategy.NoFastForward
                };

                MergeResult mergeResult = repo.MergeFetchedRefs(Constants.Signature, mergeOptions);
                Assert.Equal(mergeResult.Status, MergeStatus.NonFastForward);
            }
        }
    }
}
