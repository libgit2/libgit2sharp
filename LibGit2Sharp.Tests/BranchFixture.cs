using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class BranchFixture : BaseFixture
    {
        private readonly string[] expectedBranches = new[] { "br2", "master", "packed", "packed-test", "test", };

        [Theory]
        [InlineData("unit_test")]
        [InlineData("Ångström")]
        public void CanCreateBranch(string name)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                const string committish = "be3563ae3f795b2b4353bcce3a527ad0a4f7f644";

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Branch newBranch = repo.CreateBranch(name, committish);
                Assert.NotNull(newBranch);
                Assert.Equal(name, newBranch.FriendlyName);
                Assert.Equal("refs/heads/" + name, newBranch.CanonicalName);
                Assert.NotNull(newBranch.Tip);
                Assert.Equal(committish, newBranch.Tip.Sha);

                // Note the call to String.Normalize(). This is because, on Mac OS X, the filesystem
                // decomposes the UTF-8 characters on write, which results in a different set of bytes
                // when they're read back:
                // - from InlineData: C5-00-6E-00-67-00-73-00-74-00-72-00-F6-00-6D-00
                // - from filesystem: 41-00-0A-03-6E-00-67-00-73-00-74-00-72-00-6F-00-08-03-6D-00
                Assert.NotNull(repo.Branches.SingleOrDefault(p => p.FriendlyName.Normalize() == name));

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  "branch: Created from " + committish,
                                  null,
                                  newBranch.Tip.Id,
                                  Constants.Identity, before);

                repo.Branches.Remove(newBranch.FriendlyName);
                Assert.Null(repo.Branches[name]);
            }
        }

        [Theory]
        [InlineData("32eab9cb1f450b5fe7ab663462b77d7f4b703344")]
        public void CanHeadBeDetached(string commit)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.False(repo.Info.IsHeadDetached);
                Commands.Checkout(repo, commit);
                Assert.True(repo.Info.IsHeadDetached);
            }
        }

        [Fact]
        public void CanCreateAnUnbornBranch()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // No branch named orphan
                Assert.Null(repo.Branches["orphan"]);

                // HEAD doesn't point to an unborn branch
                Assert.False(repo.Info.IsHeadUnborn);

                // Let's move the HEAD to this branch to be created
                repo.Refs.UpdateTarget("HEAD", "refs/heads/orphan");
                Assert.True(repo.Info.IsHeadUnborn);

                // The branch still doesn't exist
                Assert.Null(repo.Branches["orphan"]);

                // Create a commit against HEAD
                Commit c = repo.Commit("New initial root commit", Constants.Signature, Constants.Signature);

                // Ensure this commit has no parent
                Assert.Empty(c.Parents);

                // The branch now exists...
                Branch orphan = repo.Branches["orphan"];
                Assert.NotNull(orphan);
                AssertBelongsToARepository(repo, orphan);

                // ...and points to that newly created commit
                Assert.Equal(c, orphan.Tip);
            }
        }

        [Fact]
        public void CanCreateBranchUsingAbbreviatedSha()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                const string name = "unit_test";
                const string committish = "be3563a";

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Branch newBranch = repo.CreateBranch(name, committish);
                Assert.Equal("refs/heads/" + name, newBranch.CanonicalName);
                Assert.Equal("be3563ae3f795b2b4353bcce3a527ad0a4f7f644", newBranch.Tip.Sha);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  "branch: Created from " + committish,
                                  null,
                                  newBranch.Tip.Id,
                                  Constants.Identity, before);
            }
        }

        [Theory]
        [InlineData("32eab9cb1f450b5fe7ab663462b77d7f4b703344")]
        [InlineData("master")]
        public void CanCreateBranchFromImplicitHead(string headCommitOrBranchSpec)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                Commands.Checkout(repo, headCommitOrBranchSpec);

                const string name = "unit_test";

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Branch newBranch = repo.CreateBranch(name);
                Assert.NotNull(newBranch);
                Assert.Equal(name, newBranch.FriendlyName);
                Assert.Equal("refs/heads/" + name, newBranch.CanonicalName);
                Assert.False(newBranch.IsCurrentRepositoryHead);
                Assert.NotNull(newBranch.Tip);
                Assert.Equal("32eab9cb1f450b5fe7ab663462b77d7f4b703344", newBranch.Tip.Sha);
                Assert.NotNull(repo.Branches.SingleOrDefault(p => p.FriendlyName == name));

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  "branch: Created from " + headCommitOrBranchSpec,
                                  null,
                                  newBranch.Tip.Id,
                                  Constants.Identity, before);
            }
        }

        [Theory]
        [InlineData("32eab9cb1f450b5fe7ab663462b77d7f4b703344")]
        [InlineData("master")]
        public void CanCreateBranchFromExplicitHead(string headCommitOrBranchSpec)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                Commands.Checkout(repo, headCommitOrBranchSpec);

                const string name = "unit_test";

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Branch newBranch = repo.CreateBranch(name, "HEAD");
                Assert.NotNull(newBranch);
                Assert.Equal("32eab9cb1f450b5fe7ab663462b77d7f4b703344", newBranch.Tip.Sha);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  "branch: Created from HEAD",
                                  null,
                                  newBranch.Tip.Id,
                                  Constants.Identity, before);
            }
        }

        [Fact]
        public void CanCreateBranchFromCommit()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                const string name = "unit_test";
                var commit = repo.Lookup<Commit>("HEAD");

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Branch newBranch = repo.CreateBranch(name, commit);
                Assert.NotNull(newBranch);
                Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", newBranch.Tip.Sha);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  "branch: Created from " + newBranch.Tip.Sha,
                                  null,
                                  newBranch.Tip.Id,
                                  Constants.Identity, before);
            }
        }

        [Fact]
        public void CanCreateBranchFromRevparseSpec()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                const string name = "revparse_branch";
                const string committish = "master~2";

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Branch newBranch = repo.CreateBranch(name, committish);
                Assert.NotNull(newBranch);
                Assert.Equal("9fd738e8f7967c078dceed8190330fc8648ee56a", newBranch.Tip.Sha);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  "branch: Created from " + committish,
                                  null,
                                  newBranch.Tip.Id,
                                  Constants.Identity, before);
            }
        }

        [Theory]
        [InlineData("test")]
        [InlineData("refs/tags/test")]
        public void CreatingABranchFromATagPeelsToTheCommit(string committish)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                const string name = "i-peel-tag";

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Branch newBranch = repo.CreateBranch(name, committish);
                Assert.NotNull(newBranch);
                Assert.Equal("e90810b8df3e80c413d903f631643c716887138d", newBranch.Tip.Sha);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  "branch: Created from " + committish,
                                  null,
                                  newBranch.Tip.Id,
                                  Constants.Identity, before);
            }
        }

        [Fact]
        public void CreatingABranchTriggersTheCreationOfADirectReference()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch newBranch = repo.CreateBranch("clone-of-master");
                Assert.False(newBranch.IsCurrentRepositoryHead);

                ObjectId commitId = repo.Head.Tip.Id;
                Assert.Equal(commitId, newBranch.Tip.Id);

                Reference reference = repo.Refs[newBranch.CanonicalName];
                Assert.NotNull(reference);
                Assert.IsType<DirectReference>(reference);
            }
        }

        [Fact]
        public void CreatingABranchFromANonCommitObjectThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string name = "sorry-dude-i-do-not-do-blobs-nor-trees";
                Assert.Throws<InvalidSpecificationException>(() => repo.CreateBranch(name, "refs/tags/point_to_blob"));
                Assert.Throws<InvalidSpecificationException>(() => repo.CreateBranch(name, "53fc32d"));
                Assert.Throws<InvalidSpecificationException>(() => repo.CreateBranch(name, "0266163"));
            }
        }

        [Fact]
        public void CreatingBranchWithUnknownNamedTargetThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => repo.Branches.Add("my_new_branch", "my_old_branch"));
            }
        }

        [Fact]
        public void CreatingBranchWithUnknownShaTargetThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() => repo.Branches.Add("my_new_branch", Constants.UnknownSha));
                Assert.Throws<NotFoundException>(() => repo.Branches.Add("my_new_branch", Constants.UnknownSha.Substring(0, 7)));
            }
        }

        [Fact]
        public void CreatingBranchWithBadParamsThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Add(null, repo.Head.CanonicalName));
                Assert.Throws<ArgumentException>(() => repo.Branches.Add(string.Empty, repo.Head.CanonicalName));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Add("bad_branch", default(string)));
                Assert.Throws<ArgumentException>(() => repo.Branches.Add("bad_branch", string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.CreateBranch("bad_branch", default(Commit)));
            }
        }

        [Fact]
        public void CanListAllBranches()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Equal(expectedBranches, SortedBranches(repo.Branches, b => b.FriendlyName));

                Assert.Equal(5, repo.Branches.Count());
            }
        }

        [Fact]
        public void CanListBranchesWithRemoteAndLocalBranchWithSameShortName()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // Create a local branch with the same short name as a remote branch.
                repo.Branches.Add("origin/master", repo.Branches["origin/test"].Tip);

                var expectedWdBranches = new[]
                                             {
                                                 "diff-test-cases", "i-do-numbers", "logo", "master", "origin/master", "track-local", "treesame_as_32eab"
                                             };

                Assert.Equal(expectedWdBranches,
                             SortedBranches(repo.Branches.Where(b => !b.IsRemote), b => b.FriendlyName));
            }
        }

        [Fact]
        public void CanListAllBranchesWhenGivenWorkingDir()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var expectedWdBranches = new[]
                                             {
                                                 "diff-test-cases", "i-do-numbers", "logo", "master", "track-local", "treesame_as_32eab",
                                                 "origin/HEAD", "origin/br2", "origin/master", "origin/packed-test",
                                                 "origin/test"
                                             };

                Assert.Equal(expectedWdBranches, SortedBranches(repo.Branches, b => b.FriendlyName));
            }
        }

        [Fact]
        public void CanListAllBranchesIncludingRemoteRefs()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var expectedBranchesIncludingRemoteRefs = new[]
                                                              {
                                                                  new { Name = "diff-test-cases", Sha = "e7039e6d0e7dd4d4c1e2e8e5aa5306b2776436ca", IsRemote = false },
                                                                  new { Name = "i-do-numbers", Sha = "7252fe2da2c4dd6d85231a150d0485ec46abaa7a", IsRemote = false },
                                                                  new { Name = "logo", Sha = "a447ba2ca8fffd46dece72f7db6faf324afb8fcd", IsRemote = false },
                                                                  new { Name = "master", Sha = "32eab9cb1f450b5fe7ab663462b77d7f4b703344", IsRemote = false },
                                                                  new { Name = "track-local", Sha = "580c2111be43802dab11328176d94c391f1deae9", IsRemote = false },
                                                                  new { Name = "treesame_as_32eab", Sha = "f705abffe7015f2beacf2abe7a36583ebee3487e", IsRemote = false },
                                                                  new { Name = "origin/HEAD", Sha = "580c2111be43802dab11328176d94c391f1deae9", IsRemote = true },
                                                                  new { Name = "origin/br2", Sha = "a4a7dce85cf63874e984719f4fdd239f5145052f", IsRemote = true },
                                                                  new { Name = "origin/master", Sha = "580c2111be43802dab11328176d94c391f1deae9", IsRemote = true },
                                                                  new { Name = "origin/packed-test", Sha = "4a202b346bb0fb0db7eff3cffeb3c70babbd2045", IsRemote = true },
                                                                  new { Name = "origin/test", Sha = "e90810b8df3e80c413d903f631643c716887138d", IsRemote = true },
                                                              };
                Assert.Equal(expectedBranchesIncludingRemoteRefs,
                             SortedBranches(repo.Branches, b => new { Name = b.FriendlyName, b.Tip.Sha, b.IsRemote }));
            }
        }

        [Fact]
        public void CanResolveRemote()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.Equal("origin", master.RemoteName);
            }
        }

        [Fact]
        public void RemoteAndUpstreamBranchCanonicalNameForNonTrackingBranchIsNull()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch test = repo.Branches["i-do-numbers"];
                Assert.Null(test.RemoteName);
                Assert.Null(test.UpstreamBranchCanonicalName);
            }
        }

        [Fact]
        public void QueryRemoteForLocalTrackingBranch()
        {
            // There is not a Remote to resolve for a local tracking branch.
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch trackLocal = repo.Branches["track-local"];
                Assert.Null(trackLocal.RemoteName);
            }
        }

        [Fact]
        public void QueryUpstreamBranchCanonicalNameForLocalTrackingBranch()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch trackLocal = repo.Branches["track-local"];
                Assert.Equal("refs/heads/master", trackLocal.UpstreamBranchCanonicalName);
            }
        }

        [Fact]
        public void QueryRemoteForRemoteBranch()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var master = repo.Branches["origin/master"];
                Assert.Equal("origin", master.RemoteName);
            }
        }

        [Fact]
        public void QueryUnresolvableRemoteForRemoteBranch()
        {
            var fetchRefSpecs = new string[] { "+refs/heads/notfound/*:refs/remotes/origin/notfound/*" };

            var path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // Update the remote config such that the remote for a
                // remote branch cannot be resolved
                Remote remote = repo.Network.Remotes["origin"];
                Assert.NotNull(remote);

                repo.Network.Remotes.Update("origin", r => r.FetchRefSpecs = fetchRefSpecs);

                Branch branch = repo.Branches["refs/remotes/origin/master"];

                Assert.NotNull(branch);
                Assert.True(branch.IsRemote);

                Assert.Null(branch.RemoteName);
            }
        }

        [Fact]
        public void QueryAmbigousRemoteForRemoteBranch()
        {
            const string fetchRefSpec = "+refs/heads/*:refs/remotes/origin/*";
            const string url = "http://github.com/libgit2/TestGitRepository";

            var path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // Add a second remote so that it is ambiguous which remote
                // the remote-tracking branch tracks.
                repo.Network.Remotes.Add("ambiguous", url, fetchRefSpec);

                Branch branch = repo.Branches["refs/remotes/origin/master"];

                Assert.NotNull(branch);
                Assert.True(branch.IsRemote);

                Assert.Null(branch.RemoteName);
            }
        }

        [Fact]
        public void CanLookupABranchByItsCanonicalName()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch branch = repo.Branches["refs/heads/br2"];
                Assert.NotNull(branch);
                Assert.Equal("br2", branch.FriendlyName);

                Branch branch2 = repo.Branches["refs/heads/br2"];
                Assert.NotNull(branch2);
                Assert.Equal("br2", branch2.FriendlyName);

                Assert.Equal(branch, branch2);
                Assert.True((branch2 == branch));
            }
        }

        [Fact]
        public void CanLookupLocalBranch()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.NotNull(master);
                Assert.False(master.IsRemote);
                Assert.Equal("master", master.FriendlyName);
                Assert.Equal("refs/heads/master", master.CanonicalName);
                Assert.True(master.IsCurrentRepositoryHead);
                Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", master.Tip.Sha);
            }
        }

        [Fact]
        public void CanLookupABranchWhichNameIsMadeOfNon7BitsAsciiCharacters()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string name = "Ångström";
                Branch newBranch = repo.CreateBranch(name, "be3563a");
                Assert.NotNull(newBranch);

                Branch retrieved = repo.Branches["Ångström"];
                Assert.NotNull(retrieved);
                Assert.Equal(newBranch.Tip, retrieved.Tip);
            }
        }

        [Fact]
        public void LookingOutABranchByNameWithBadParamsThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch branch;
                Assert.Throws<ArgumentNullException>(() => branch = repo.Branches[null]);
                Assert.Throws<ArgumentException>(() => branch = repo.Branches[""]);
            }
        }

        [Fact]
        public void CanGetInformationFromUnbornBranch()
        {
            string repoPath = InitNewRepository(true);

            using (var repo = new Repository(repoPath))
            {
                var head = repo.Head;

                Assert.Equal("refs/heads/master", head.CanonicalName);
                Assert.Empty(head.Commits);
                Assert.True(head.IsCurrentRepositoryHead);
                Assert.False(head.IsRemote);
                Assert.Equal("master", head.FriendlyName);
                Assert.Null(head.Tip);
                Assert.Null(head["huh?"]);

                Assert.False(head.IsTracking);
                Assert.Null(head.TrackedBranch);

                Assert.NotNull(head.TrackingDetails);
                Assert.Null(head.TrackingDetails.AheadBy);
                Assert.Null(head.TrackingDetails.BehindBy);
                Assert.Null(head.TrackingDetails.CommonAncestor);
            }
        }

        [Fact]
        public void CanGetTrackingInformationFromBranchSharingNoHistoryWithItsTrackedBranch()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                const string logMessage = "update target message";
                repo.Refs.UpdateTarget("refs/remotes/origin/master", "origin/test", logMessage);

                Assert.True(master.IsTracking);
                Assert.NotNull(master.TrackedBranch);
                AssertBelongsToARepository(repo, master.TrackedBranch);

                Assert.NotNull(master.TrackingDetails);
                Assert.Equal(9, master.TrackingDetails.AheadBy);
                Assert.Equal(2, master.TrackingDetails.BehindBy);
                Assert.Null(repo.Head.TrackingDetails.CommonAncestor);

                // Assert reflog entry is created
                var reflogEntry = repo.Refs.Log("refs/remotes/origin/master").First();
                Assert.Equal(repo.Branches["origin/test"].Tip.Id, reflogEntry.To);
                Assert.Equal(logMessage, reflogEntry.Message);
            }
        }

        [Fact]
        public void TrackingInformationIsEmptyForBranchTrackingPrunedRemoteBranch()
        {
            var path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string remoteRef = "refs/remotes/origin/master";
                repo.Refs.Remove(remoteRef);

                Branch master = repo.Branches["master"];
                Assert.True(master.IsTracking);
                Assert.NotNull(master.TrackedBranch);
                Assert.Equal(remoteRef, master.TrackedBranch.CanonicalName);
                Assert.Null(master.TrackedBranch.Tip);

                Assert.NotNull(master.TrackingDetails);
                Assert.Null(master.TrackingDetails.AheadBy);
                Assert.Null(master.TrackingDetails.BehindBy);
                Assert.Null(master.TrackingDetails.CommonAncestor);
            }
        }

        [Fact]
        public void TrackingInformationIsEmptyForNonTrackingBranch()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch branch = repo.Branches["test"];
                Assert.False(branch.IsTracking);
                Assert.Null(branch.TrackedBranch);

                Assert.NotNull(branch.TrackingDetails);
                Assert.Null(branch.TrackingDetails.AheadBy);
                Assert.Null(branch.TrackingDetails.BehindBy);
                Assert.Null(branch.TrackingDetails.CommonAncestor);
            }
        }

        [Fact]
        public void CanGetTrackingInformationForTrackingBranch()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.True(master.IsTracking);
                Assert.Equal(repo.Branches["refs/remotes/origin/master"], master.TrackedBranch);

                Assert.NotNull(master.TrackingDetails);
                Assert.Equal(2, master.TrackingDetails.AheadBy);
                Assert.Equal(2, master.TrackingDetails.BehindBy);
                Assert.Equal(repo.Lookup<Commit>("4c062a6"), master.TrackingDetails.CommonAncestor);
            }
        }

        [Fact]
        public void CanGetTrackingInformationForLocalTrackingBranch()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var branch = repo.Branches["track-local"];
                Assert.True(branch.IsTracking);
                Assert.Equal(repo.Branches["master"], branch.TrackedBranch);

                Assert.NotNull(branch.TrackingDetails);
                Assert.Equal(2, branch.TrackingDetails.AheadBy);
                Assert.Equal(2, branch.TrackingDetails.BehindBy);
                Assert.Equal(repo.Lookup<Commit>("4c062a6"), branch.TrackingDetails.CommonAncestor);
            }
        }

        [Fact]
        public void RenamingARemoteTrackingBranchThrows()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["refs/remotes/origin/master"];
                Assert.True(master.IsRemote);

                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Rename(master, "new_name", true));
            }
        }

        [Fact]
        public void CanWalkCommitsFromAnotherBranch()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["test"];
                Assert.Equal(2, master.Commits.Count());
            }
        }

        [Fact]
        public void CanSetTrackedBranch()
        {
            const string testBranchName = "branchToSetUpstreamInfoFor";
            const string trackedBranchName = "refs/remotes/origin/master";

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch trackedBranch = repo.Branches[trackedBranchName];
                Assert.True(trackedBranch.IsRemote);

                Branch branch = repo.CreateBranch(testBranchName, trackedBranch.Tip);
                Assert.False(branch.IsTracking);

                repo.Branches.Update(branch,
                    b => b.TrackedBranch = trackedBranch.CanonicalName);

                // Verify the immutability of the branch.
                Assert.False(branch.IsTracking);

                // Get the updated branch information.
                branch = repo.Branches[testBranchName];

                Remote upstreamRemote = repo.Network.Remotes["origin"];
                Assert.NotNull(upstreamRemote);

                Assert.True(branch.IsTracking);
                Assert.Equal(trackedBranch, branch.TrackedBranch);
                Assert.Equal("origin", branch.RemoteName);
            }
        }

        [Fact]
        public void SetTrackedBranchForUnreasolvableRemoteThrows()
        {
            const string testBranchName = "branchToSetUpstreamInfoFor";
            const string trackedBranchName = "refs/remotes/origin/master";
            var fetchRefSpecs = new string[] { "+refs/heads/notfound/*:refs/remotes/origin/notfound/*" };

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // Modify the fetch spec so that the remote for the remote-tracking branch
                // cannot be resolved.
                Remote remote = repo.Network.Remotes["origin"];
                Assert.NotNull(remote);
                repo.Network.Remotes.Update("origin", r => r.FetchRefSpecs = fetchRefSpecs);

                // Now attempt to update the tracked branch
                Branch branch = repo.CreateBranch(testBranchName);
                Assert.False(branch.IsTracking);

                Branch trackedBranch = repo.Branches[trackedBranchName];

                Assert.Throws<NotFoundException>(() => repo.Branches.Update(branch,
                                                                                b => b.TrackedBranch = trackedBranch.CanonicalName));
            }
        }

        [Fact]
        public void CanSetUpstreamBranch()
        {
            const string testBranchName = "branchToSetUpstreamInfoFor";
            const string upstreamBranchName = "refs/heads/master";
            const string trackedBranchName = "refs/remotes/origin/master";
            const string remoteName = "origin";

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch trackedBranch = repo.Branches[trackedBranchName];
                Assert.True(trackedBranch.IsRemote);

                Branch branch = repo.CreateBranch(testBranchName, trackedBranch.Tip);
                Assert.False(branch.IsTracking);

                Branch updatedBranch = repo.Branches.Update(branch,
                    b => b.Remote = remoteName,
                    b => b.UpstreamBranch = upstreamBranchName);

                // Verify the immutability of the branch.
                Assert.False(branch.IsTracking);

                Remote upstreamRemote = repo.Network.Remotes[remoteName];
                Assert.NotNull(upstreamRemote);

                Assert.True(updatedBranch.IsTracking);
                Assert.Equal(trackedBranch, updatedBranch.TrackedBranch);
                Assert.Equal(upstreamBranchName, updatedBranch.UpstreamBranchCanonicalName);
                Assert.Equal(remoteName, updatedBranch.RemoteName);
            }
        }

        [Fact]
        public void CanSetLocalTrackedBranch()
        {
            const string testBranchName = "branchToSetUpstreamInfoFor";
            const string localTrackedBranchName = "refs/heads/master";

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch trackedBranch = repo.Branches[localTrackedBranchName];
                Assert.False(trackedBranch.IsRemote);

                Branch branch = repo.CreateBranch(testBranchName, trackedBranch.Tip);
                Assert.False(branch.IsTracking);

                repo.Branches.Update(branch,
                    b => b.TrackedBranch = trackedBranch.CanonicalName);

                // Get the updated branch information.
                branch = repo.Branches[testBranchName];

                // Branches that track the local remote do not have the "Remote" property set.
                // Verify (through the configuration entry) that the local remote is set as expected.
                Assert.Null(branch.RemoteName);
                ConfigurationEntry<string> remoteConfigEntry = repo.Config.Get<string>("branch", testBranchName, "remote");
                Assert.NotNull(remoteConfigEntry);
                Assert.Equal(".", remoteConfigEntry.Value);

                ConfigurationEntry<string> mergeConfigEntry = repo.Config.Get<string>("branch", testBranchName, "merge");
                Assert.NotNull(mergeConfigEntry);
                Assert.Equal("refs/heads/master", mergeConfigEntry.Value);

                // Verify the IsTracking and TrackedBranch properties.
                Assert.True(branch.IsTracking);
                Assert.Equal(trackedBranch, branch.TrackedBranch);
                Assert.Equal("refs/heads/master", branch.UpstreamBranchCanonicalName);
            }
        }

        [Fact]
        public void CanUnsetTrackedBranch()
        {
            const string testBranchName = "branchToSetUpstreamInfoFor";
            const string trackedBranchName = "refs/remotes/origin/master";

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch trackedBranch = repo.Branches[trackedBranchName];

                Branch branch = repo.CreateBranch(testBranchName, trackedBranch.Tip);
                Assert.False(branch.IsTracking);

                branch = repo.Branches.Update(branch,
                    b => b.TrackedBranch = trackedBranch.CanonicalName);

                // Got the updated branch from the Update() method
                Assert.True(branch.IsTracking);

                branch = repo.Branches.Update(branch,
                    b => b.TrackedBranch = null);

                // Verify this is no longer a tracking branch
                Assert.False(branch.IsTracking);
                Assert.Null(branch.RemoteName);
                Assert.Null(branch.UpstreamBranchCanonicalName);
            }
        }

        [Fact]
        public void CanWalkCommitsFromBranch()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                Assert.Equal(7, master.Commits.Count());
            }
        }

        private void AssertRemoval(string branchName, bool isRemote, bool shouldPreviouslyAssertExistence)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                if (shouldPreviouslyAssertExistence)
                {
                    Assert.NotNull(repo.Branches[branchName]);
                }

                repo.Branches.Remove(branchName, isRemote);
                Branch branch = repo.Branches[branchName];
                Assert.Null(branch);
            }
        }

        [Theory]
        [InlineData("i-do-numbers", false)]
        [InlineData("origin/br2", true)]
        public void CanRemoveAnExistingNamedBranch(string branchName, bool isRemote)
        {
            AssertRemoval(branchName, isRemote, true);
        }

        [Theory]
        [InlineData("i-do-numbers")]
        [InlineData("origin/br2")]
        public void CanRemoveAnExistingBranch(string branchName)
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch curBranch = repo.Branches[branchName];

                repo.Branches.Remove(curBranch);
                Branch branch = repo.Branches[branchName];
                Assert.Null(branch);
            }
        }

        [Fact]
        public void CanCreateBranchInDeletedNestedBranchNamespace()
        {
            const string namespaceName = "level_one";
            string branchWithNamespaceName = string.Join("/", namespaceName, "level_two");

            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Commit commit = repo.Head.Tip;

                Branch branchWithNamespace = repo.Branches.Add(branchWithNamespaceName, commit);
                repo.Branches.Remove(branchWithNamespace);

                repo.Branches.Add(namespaceName, commit);
            }
        }

        [Theory]
        [InlineData("I-donot-exist", false)]
        [InlineData("me/neither", true)]
        public void CanRemoveANonExistingBranch(string branchName, bool isRemote)
        {
            AssertRemoval(branchName, isRemote, false);
        }

        [Fact]
        public void RemovingABranchWhichIsTheCurrentHeadThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Remove(repo.Head.FriendlyName));
            }
        }

        [Fact]
        public void RemovingABranchWithBadParamsThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Branches.Remove(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Remove(default(string)));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Remove(default(Branch)));
            }
        }

        [Fact]
        public void OnlyOneBranchIsTheHead()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch head = null;

                foreach (Branch branch in repo.Branches)
                {
                    bool isHead = branch.IsCurrentRepositoryHead;

                    if (!isHead)
                    {
                        continue;
                    }

                    if (head == null)
                    {
                        head = branch;
                        continue;
                    }

                    Assert.Fail(string.Format("Both '{0}' and '{1}' appear to be Head.", head.CanonicalName, branch.CanonicalName));
                }

                Assert.NotNull(head);
            }
        }

        [Fact]
        public void TwoBranchesPointingAtTheSameCommitAreNotBothCurrent()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["refs/heads/master"];

                Branch newBranch = repo.Branches.Add("clone-of-master", master.Tip.Sha);
                Assert.False(newBranch.IsCurrentRepositoryHead);
            }
        }

        [Fact]
        public void CanRenameABranch()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                Assert.Null(repo.Branches["br3"]);
                var br2 = repo.Branches["br2"];
                Assert.NotNull(br2);

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Branch newBranch = repo.Branches.Rename("br2", "br3");

                Assert.Equal("br3", newBranch.FriendlyName);

                Assert.Null(repo.Branches["br2"]);
                Assert.NotNull(repo.Branches["br3"]);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  string.Format("branch: renamed {0} to {1}", br2.CanonicalName, newBranch.CanonicalName),
                                  br2.Tip.Id,
                                  newBranch.Tip.Id,
                                  Constants.Identity, before);
            }
        }

        [Fact]
        public void BlindlyRenamingABranchOverAnExistingOneThrows()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NameConflictException>(() => repo.Branches.Rename("br2", "test"));
            }
        }

        [Fact]
        public void CanRenameABranchWhileOverwritingAnExistingOne()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                Branch test = repo.Branches["test"];
                Assert.NotNull(test);

                Branch br2 = repo.Branches["br2"];
                Assert.NotNull(br2);

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                Branch newBranch = repo.Branches.Rename("br2", "test", true);
                Assert.Equal("test", newBranch.FriendlyName);

                Assert.Null(repo.Branches["br2"]);

                Branch newTest = repo.Branches["test"];
                Assert.NotNull(newTest);
                Assert.Equal(newBranch, newTest);

                Assert.Equal(br2.Tip, newTest.Tip);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  string.Format("branch: renamed {0} to {1}", br2.CanonicalName, newBranch.CanonicalName),
                                  br2.Tip.Id,
                                  newTest.Tip.Id,
                                  Constants.Identity, before);
            }
        }

        [Fact]
        public void DetachedHeadIsNotATrackingBranch()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                string headSha = repo.Head.Tip.Sha;
                Commands.Checkout(repo, headSha);

                Assert.False(repo.Head.IsTracking);
                Assert.Null(repo.Head.TrackedBranch);

                Assert.NotNull(repo.Head.TrackingDetails);
                Assert.Null(repo.Head.TrackingDetails.AheadBy);
                Assert.Null(repo.Head.TrackingDetails.BehindBy);
                Assert.Null(repo.Head.TrackingDetails.CommonAncestor);
            }
        }

        [Fact]
        public void TrackedBranchExistsFromDefaultConfigInEmptyClone()
        {
            string repoPath = InitNewRepository(true);

            Uri uri;

            using (var emptyRepo = new Repository(repoPath))
            {
                uri = new Uri($"file://{emptyRepo.Info.Path}");
            }

            SelfCleaningDirectory scd2 = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(uri.AbsoluteUri, scd2.DirectoryPath);

            using (var repo = new Repository(clonedRepoPath))
            {
                Assert.Empty(Directory.GetFiles(scd2.RootedDirectoryPath));
                Assert.Equal("master", repo.Head.FriendlyName);

                Assert.Null(repo.Head.Tip);
                Assert.NotNull(repo.Head.TrackedBranch);
                Assert.Null(repo.Head.TrackedBranch.Tip);

                Assert.NotNull(repo.Head.TrackingDetails);
                Assert.Null(repo.Head.TrackingDetails.AheadBy);
                Assert.Null(repo.Head.TrackingDetails.BehindBy);
                Assert.Null(repo.Head.TrackingDetails.CommonAncestor);

                Assert.Equal("origin", repo.Head.RemoteName);

                Touch(repo.Info.WorkingDirectory, "a.txt", "a");
                Commands.Stage(repo, "a.txt");
                repo.Commit("A file", Constants.Signature, Constants.Signature);

                Assert.NotNull(repo.Head.Tip);
                Assert.NotNull(repo.Head.TrackedBranch);
                Assert.Null(repo.Head.TrackedBranch.Tip);

                Assert.NotNull(repo.Head.TrackingDetails);
                Assert.Null(repo.Head.TrackingDetails.AheadBy);
                Assert.Null(repo.Head.TrackingDetails.BehindBy);
                Assert.Null(repo.Head.TrackingDetails.CommonAncestor);
            }
        }

        [Fact]
        public void RemoteBranchesDoNotTrackAnything()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var branches = repo.Branches.Where(b => b.IsRemote);

                foreach (var branch in branches)
                {
                    Assert.True(branch.IsRemote);
                    Assert.NotNull(branch.RemoteName);
                    Assert.False(branch.IsTracking);
                    Assert.Null(branch.TrackedBranch);

                    Assert.NotNull(branch.TrackingDetails);
                    Assert.Null(branch.TrackingDetails.AheadBy);
                    Assert.Null(branch.TrackingDetails.BehindBy);
                    Assert.Null(branch.TrackingDetails.CommonAncestor);
                }
            }
        }

        private static T[] SortedBranches<T>(IEnumerable<Branch> branches, Func<Branch, T> selector)
        {
            return branches.OrderBy(b => b.CanonicalName, StringComparer.Ordinal).Select(selector).ToArray();
        }

        [Fact]
        public void CreatingABranchIncludesTheCorrectReflogEntries()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                var branch = repo.Branches.Add("foo", repo.Head.Tip);

                AssertRefLogEntry(repo, branch.CanonicalName,
                                  string.Format("branch: Created from {0}", repo.Head.Tip.Sha),
                                  null, branch.Tip.Id,
                                  Constants.Identity, before);

                before = DateTimeOffset.Now.TruncateMilliseconds();

                branch = repo.Branches.Add("bar", repo.Head.Tip);

                AssertRefLogEntry(repo, branch.CanonicalName,
                                  "branch: Created from " + repo.Head.Tip.Sha,
                                  null, repo.Head.Tip.Id,
                                  Constants.Identity, before);
            }
        }

        [Fact]
        public void RenamingABranchIncludesTheCorrectReflogEntries()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path, new RepositoryOptions { Identity = Constants.Identity }))
            {
                EnableRefLog(repo);
                var master = repo.Branches["master"];

                var before = DateTimeOffset.Now.TruncateMilliseconds();

                var newMaster = repo.Branches.Rename(master, "new-master");
                AssertRefLogEntry(repo, newMaster.CanonicalName, "branch: renamed refs/heads/master to refs/heads/new-master",
                                  newMaster.Tip.Id, newMaster.Tip.Id,
                                  Constants.Identity, before);

                before = DateTimeOffset.Now.TruncateMilliseconds();

                var newMaster2 = repo.Branches.Rename(newMaster, "new-master2");
                AssertRefLogEntry(repo, newMaster2.CanonicalName, "branch: renamed refs/heads/new-master to refs/heads/new-master2",
                                  newMaster.Tip.Id, newMaster2.Tip.Id,
                                  Constants.Identity, before);
            }
        }
    }
}
