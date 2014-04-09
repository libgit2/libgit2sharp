using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

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
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                const string committish = "be3563ae3f795b2b4353bcce3a527ad0a4f7f644";

                Branch newBranch = repo.CreateBranch(name, committish);
                Assert.NotNull(newBranch);
                Assert.Equal(name, newBranch.Name);
                Assert.Equal("refs/heads/" + name, newBranch.CanonicalName);
                Assert.NotNull(newBranch.Tip);
                Assert.Equal(committish, newBranch.Tip.Sha);

                // Note the call to String.Normalize(). This is because, on Mac OS X, the filesystem
                // decomposes the UTF-8 characters on write, which results in a different set of bytes
                // when they're read back:
                // - from InlineData: C5-00-6E-00-67-00-73-00-74-00-72-00-F6-00-6D-00
                // - from filesystem: 41-00-0A-03-6E-00-67-00-73-00-74-00-72-00-6F-00-08-03-6D-00
                Assert.NotNull(repo.Branches.SingleOrDefault(p => p.Name.Normalize() == name));

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  newBranch.Tip.Id,
                                  "branch: Created from " + committish);

                repo.Branches.Remove(newBranch.Name);
                Assert.Null(repo.Branches[name]);
            }
        }

        [Fact]
        public void CanCreateAnUnbornBranch()
        {
            string path = CloneStandardTestRepo();
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
                Assert.Equal(0, c.Parents.Count());

                // The branch now exists...
                Branch orphan = repo.Branches["orphan"];
                Assert.NotNull(orphan);

                // ...and points to that newly created commit
                Assert.Equal(c, orphan.Tip);
            }
        }

        [Fact]
        public void CanCreateBranchUsingAbbreviatedSha()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                const string name = "unit_test";
                const string committish = "be3563a";

                Branch newBranch = repo.CreateBranch(name, committish);
                Assert.Equal("refs/heads/" + name, newBranch.CanonicalName);
                Assert.Equal("be3563ae3f795b2b4353bcce3a527ad0a4f7f644", newBranch.Tip.Sha);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  newBranch.Tip.Id,
                                  "branch: Created from " + committish);
            }
        }

        [Theory]
        [InlineData("32eab9cb1f450b5fe7ab663462b77d7f4b703344")]
        [InlineData("master")]
        public void CanCreateBranchFromImplicitHead(string headCommitOrBranchSpec)
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                repo.Checkout(headCommitOrBranchSpec);

                const string name = "unit_test";
                Branch newBranch = repo.CreateBranch(name);
                Assert.NotNull(newBranch);
                Assert.Equal(name, newBranch.Name);
                Assert.Equal("refs/heads/" + name, newBranch.CanonicalName);
                Assert.False(newBranch.IsCurrentRepositoryHead);
                Assert.NotNull(newBranch.Tip);
                Assert.Equal("32eab9cb1f450b5fe7ab663462b77d7f4b703344", newBranch.Tip.Sha);
                Assert.NotNull(repo.Branches.SingleOrDefault(p => p.Name == name));

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  newBranch.Tip.Id,
                                  "branch: Created from " + headCommitOrBranchSpec);
            }
        }

        [Theory]
        [InlineData("32eab9cb1f450b5fe7ab663462b77d7f4b703344")]
        [InlineData("master")]
        public void CanCreateBranchFromExplicitHead(string headCommitOrBranchSpec)
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                repo.Checkout(headCommitOrBranchSpec);

                const string name = "unit_test";
                Branch newBranch = repo.CreateBranch(name, "HEAD");
                Assert.NotNull(newBranch);
                Assert.Equal("32eab9cb1f450b5fe7ab663462b77d7f4b703344", newBranch.Tip.Sha);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  newBranch.Tip.Id,
                                  "branch: Created from " + headCommitOrBranchSpec);
            }
        }

        [Fact]
        public void CanCreateBranchFromCommit()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                const string name = "unit_test";
                var commit = repo.Lookup<Commit>("HEAD");
                Branch newBranch = repo.CreateBranch(name, commit);
                Assert.NotNull(newBranch);
                Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", newBranch.Tip.Sha);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  newBranch.Tip.Id,
                                  "branch: Created from " + newBranch.Tip.Sha);
            }
        }

        [Fact]
        public void CanCreateBranchFromRevparseSpec()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                const string name = "revparse_branch";
                const string committish = "master~2";

                Branch newBranch = repo.CreateBranch(name, committish);
                Assert.NotNull(newBranch);
                Assert.Equal("9fd738e8f7967c078dceed8190330fc8648ee56a", newBranch.Tip.Sha);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  newBranch.Tip.Id,
                                  "branch: Created from " + committish);
            }
        }

        [Theory]
        [InlineData("test")]
        [InlineData("refs/tags/test")]
        public void CreatingABranchFromATagPeelsToTheCommit(string committish)
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                const string name = "i-peel-tag";

                Branch newBranch = repo.CreateBranch(name, committish);
                Assert.NotNull(newBranch);
                Assert.Equal("e90810b8df3e80c413d903f631643c716887138d", newBranch.Tip.Sha);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  newBranch.Tip.Id,
                                  "branch: Created from " + committish);
            }
        }

        [Fact]
        public void CreatingABranchTriggersTheCreationOfADirectReference()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch newBranch = repo.CreateBranch("clone-of-master");
                Assert.False(newBranch.IsCurrentRepositoryHead);

                ObjectId commitId = repo.Head.Tip.Id;
                Assert.Equal(commitId, newBranch.Tip.Id);

                Reference reference = repo.Refs[newBranch.CanonicalName];
                Assert.NotNull(reference);
                Assert.IsType(typeof(DirectReference), reference);
            }
        }

        [Fact]
        public void CreatingABranchFromANonCommitObjectThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                const string name = "sorry-dude-i-do-not-do-blobs-nor-trees";
                Assert.Throws<LibGit2SharpException>(() => repo.CreateBranch(name, "refs/tags/point_to_blob"));
                Assert.Throws<LibGit2SharpException>(() => repo.CreateBranch(name, "53fc32d"));
                Assert.Throws<LibGit2SharpException>(() => repo.CreateBranch(name, "0266163"));
            }
        }

        [Fact]
        public void CreatingBranchWithUnknownNamedTargetThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Add("my_new_branch", "my_old_branch"));
            }
        }

        [Fact]
        public void CreatingBranchWithUnknownShaTargetThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Add("my_new_branch", Constants.UnknownSha));
                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Add("my_new_branch", Constants.UnknownSha.Substring(0, 7)));
            }
        }

        [Fact]
        public void CreatingBranchWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
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
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Equal(expectedBranches, SortedBranches(repo.Branches, b => b.Name));

                Assert.Equal(5, repo.Branches.Count());
            }
        }

        [Fact]
        public void CanListBranchesWithRemoteAndLocalBranchWithSameShortName()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                // Create a local branch with the same short name as a remote branch.
                repo.Branches.Add("origin/master", repo.Branches["origin/test"].Tip);

                var expectedWdBranches = new[]
                                             {
                                                 "diff-test-cases", "i-do-numbers", "logo", "master", "origin/master", "track-local", "treesame_as_32eab"
                                             };

                Assert.Equal(expectedWdBranches,
                             SortedBranches(repo.Branches.Where(b => !b.IsRemote), b => b.Name));
            }
        }

        [Fact]
        public void CanListAllBranchesWhenGivenWorkingDir()
        {
            using (var repo = new Repository(StandardTestRepoWorkingDirPath))
            {
                var expectedWdBranches = new[]
                                             {
                                                 "diff-test-cases", "i-do-numbers", "logo", "master", "track-local", "treesame_as_32eab",
                                                 "origin/HEAD", "origin/br2", "origin/master", "origin/packed-test",
                                                 "origin/test"
                                             };

                Assert.Equal(expectedWdBranches, SortedBranches(repo.Branches, b => b.Name));
            }
        }

        [Fact]
        public void CanListAllBranchesIncludingRemoteRefs()
        {
            using (var repo = new Repository(StandardTestRepoPath))
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
                             SortedBranches(repo.Branches, b => new { b.Name, b.Tip.Sha, b.IsRemote }));
            }
        }

        [Fact]
        public void CanResolveRemote()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Branch master = repo.Branches["master"];
                Assert.Equal(repo.Network.Remotes["origin"], master.Remote);
            }
        }

        [Fact]
        public void RemoteAndUpstreamBranchCanonicalNameForNonTrackingBranchIsNull()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Branch test = repo.Branches["i-do-numbers"];
                Assert.Null(test.Remote);
                Assert.Null(test.UpstreamBranchCanonicalName);
            }
        }

        [Fact]
        public void QueryRemoteForLocalTrackingBranch()
        {
            // There is not a Remote to resolve for a local tracking branch.
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Branch trackLocal = repo.Branches["track-local"];
                Assert.Null(trackLocal.Remote);
            }
        }

        [Fact]
        public void QueryUpstreamBranchCanonicalNameForLocalTrackingBranch()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Branch trackLocal = repo.Branches["track-local"];
                Assert.Equal("refs/heads/master", trackLocal.UpstreamBranchCanonicalName);
            }
        }

        [Fact]
        public void CanLookupABranchByItsCanonicalName()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Branch branch = repo.Branches["refs/heads/br2"];
                Assert.NotNull(branch);
                Assert.Equal("br2", branch.Name);

                Branch branch2 = repo.Branches["refs/heads/br2"];
                Assert.NotNull(branch2);
                Assert.Equal("br2", branch2.Name);

                Assert.Equal(branch, branch2);
                Assert.True((branch2 == branch));
            }
        }

        [Fact]
        public void CanLookupLocalBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Branch master = repo.Branches["master"];
                Assert.NotNull(master);
                Assert.False(master.IsRemote);
                Assert.Equal("master", master.Name);
                Assert.Equal("refs/heads/master", master.CanonicalName);
                Assert.True(master.IsCurrentRepositoryHead);
                Assert.Equal("4c062a6361ae6959e06292c1fa5e2822d9c96345", master.Tip.Sha);
            }
        }

        [Fact]
        public void CanLookupABranchWhichNameIsMadeOfNon7BitsAsciiCharacters()
        {
            string path = CloneBareTestRepo();
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
            using (var repo = new Repository(BareTestRepoPath))
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
                Assert.Equal(0, head.Commits.Count());
                Assert.True(head.IsCurrentRepositoryHead);
                Assert.False(head.IsRemote);
                Assert.Equal("master", head.Name);
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["master"];
                const string logMessage = "update target message";
                repo.Refs.UpdateTarget("refs/remotes/origin/master", "origin/test", Constants.Signature, logMessage);

                Assert.True(master.IsTracking);
                Assert.NotNull(master.TrackedBranch);

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
        public void TrackingInformationIsEmptyForNonTrackingBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
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
            using (var repo = new Repository(StandardTestRepoPath))
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
            using (var repo = new Repository(StandardTestRepoPath))
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
        public void MovingARemoteTrackingBranchThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Branch master = repo.Branches["refs/remotes/origin/master"];
                Assert.True(master.IsRemote);

                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Move(master, "new_name", true));
            }
        }

        [Fact]
        public void CanWalkCommitsFromAnotherBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
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

            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch branch = repo.CreateBranch(testBranchName);
                Assert.False(branch.IsTracking);

                Branch trackedBranch = repo.Branches[trackedBranchName];
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
                Assert.Equal(upstreamRemote, branch.Remote);
            }
        }

        [Fact]
        public void CanSetUpstreamBranch()
        {
            const string testBranchName = "branchToSetUpstreamInfoFor";
            const string upstreamBranchName = "refs/heads/master";
            const string trackedBranchName = "refs/remotes/origin/master";
            const string remoteName = "origin";

            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch branch = repo.CreateBranch(testBranchName);
                Assert.False(branch.IsTracking);

                Branch trackedBranch = repo.Branches[trackedBranchName];
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
                Assert.Equal(upstreamRemote, updatedBranch.Remote);
            }
        }

        [Fact]
        public void CanSetLocalTrackedBranch()
        {
            const string testBranchName = "branchToSetUpstreamInfoFor";
            const string localTrackedBranchName = "refs/heads/master";

            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch branch = repo.CreateBranch(testBranchName);
                Assert.False(branch.IsTracking);

                Branch trackedBranch = repo.Branches[localTrackedBranchName];

                repo.Branches.Update(branch,
                    b => b.TrackedBranch = trackedBranch.CanonicalName);

                // Get the updated branch information.
                branch = repo.Branches[testBranchName];

                // Branches that track the local remote do not have the "Remote" property set.
                // Verify (through the configuration entry) that the local remote is set as expected.
                Assert.Null(branch.Remote);
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

            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch branch = repo.CreateBranch(testBranchName);
                Assert.False(branch.IsTracking);

                branch = repo.Branches.Update(branch,
                    b => b.TrackedBranch = trackedBranchName);

                // Got the updated branch from the Update() method
                Assert.True(branch.IsTracking);

                branch = repo.Branches.Update(branch,
                    b => b.TrackedBranch = null);

                // Verify this is no longer a tracking branch
                Assert.False(branch.IsTracking);
                Assert.Null(branch.Remote);
                Assert.Null(branch.UpstreamBranchCanonicalName);
            }
        }

        [Fact]
        public void CanWalkCommitsFromBranch()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Branch master = repo.Branches["master"];
                Assert.Equal(7, master.Commits.Count());
            }
        }

        private void AssertRemoval(string branchName, bool isRemote, bool shouldPreviouslyAssertExistence)
        {
            string path = CloneStandardTestRepo();
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch curBranch = repo.Branches[branchName];

                repo.Branches.Remove(curBranch);
                Branch branch = repo.Branches[branchName];
                Assert.Null(branch);
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
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<LibGit2SharpException>(() => repo.Branches.Remove(repo.Head.Name));
            }
        }

        [Fact]
        public void RemovingABranchWithBadParamsThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Branches.Remove(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Branches.Remove(null));
            }
        }

        [Fact]
        public void OnlyOneBranchIsTheHead()
        {
            using (var repo = new Repository(BareTestRepoPath))
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

                    Assert.True(false, string.Format("Both '{0}' and '{1}' appear to be Head.", head.CanonicalName, branch.CanonicalName));
                }

                Assert.NotNull(head);
            }
        }

        [Fact]
        public void TwoBranchesPointingAtTheSameCommitAreNotBothCurrent()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                Branch master = repo.Branches["refs/heads/master"];

                Branch newBranch = repo.Branches.Add("clone-of-master", master.Tip.Sha);
                Assert.False(newBranch.IsCurrentRepositoryHead);
            }
        }

        [Fact]
        public void CanMoveABranch()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                Assert.Null(repo.Branches["br3"]);
                var br2 = repo.Branches["br2"];
                Assert.NotNull(br2);

                Branch newBranch = repo.Branches.Move("br2", "br3");

                Assert.Equal("br3", newBranch.Name);

                Assert.Null(repo.Branches["br2"]);
                Assert.NotNull(repo.Branches["br3"]);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  newBranch.Tip.Id,
                                  string.Format("branch: renamed {0} to {1}", br2.CanonicalName, newBranch.CanonicalName));
            }
        }

        [Fact]
        public void BlindlyMovingABranchOverAnExistingOneThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Throws<NameConflictException>(() => repo.Branches.Move("br2", "test"));
            }
        }

        [Fact]
        public void CanMoveABranchWhileOverwritingAnExistingOne()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);

                Branch test = repo.Branches["test"];
                Assert.NotNull(test);

                Branch br2 = repo.Branches["br2"];
                Assert.NotNull(br2);

                Branch newBranch = repo.Branches.Move("br2", "test", true);
                Assert.Equal("test", newBranch.Name);

                Assert.Null(repo.Branches["br2"]);

                Branch newTest = repo.Branches["test"];
                Assert.NotNull(newTest);
                Assert.Equal(newBranch, newTest);

                Assert.Equal(br2.Tip, newTest.Tip);

                AssertRefLogEntry(repo, newBranch.CanonicalName,
                                  newBranch.Tip.Id,
                                  string.Format("branch: renamed {0} to {1}", br2.CanonicalName, newBranch.CanonicalName),
                                  test.Tip.Id);
            }
        }

        [Fact]
        public void DetachedHeadIsNotATrackingBranch()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                string headSha = repo.Head.Tip.Sha;
                repo.Checkout(headSha);

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
                uri = new Uri(emptyRepo.Info.Path);
            }

            SelfCleaningDirectory scd2 = BuildSelfCleaningDirectory();

            string clonedRepoPath = Repository.Clone(uri.AbsoluteUri, scd2.DirectoryPath);

            using (var repo = new Repository(clonedRepoPath))
            {
                Assert.Empty(Directory.GetFiles(scd2.RootedDirectoryPath));
                Assert.Equal(repo.Head.Name, "master");

                Assert.Null(repo.Head.Tip);
                Assert.NotNull(repo.Head.TrackedBranch);
                Assert.Null(repo.Head.TrackedBranch.Tip);

                Assert.NotNull(repo.Head.TrackingDetails);
                Assert.Null(repo.Head.TrackingDetails.AheadBy);
                Assert.Null(repo.Head.TrackingDetails.BehindBy);
                Assert.Null(repo.Head.TrackingDetails.CommonAncestor);

                Assert.NotNull(repo.Head.Remote);
                Assert.Equal("origin", repo.Head.Remote.Name);

                Touch(repo.Info.WorkingDirectory, "a.txt", "a");
                repo.Index.Stage("a.txt");
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
            using (var repo = new Repository(StandardTestRepoPath))
            {
                var branches = repo.Branches.Where(b => b.IsRemote);

                foreach (var branch in branches)
                {
                    Assert.True(branch.IsRemote);
                    Assert.NotNull(branch.Remote);
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);
                var branch = repo.Branches.Add("foo", repo.Head.Tip);
                AssertRefLogEntry(repo, branch.CanonicalName, branch.Tip.Id,
                    string.Format("branch: Created from {0}", repo.Head.Tip.Sha));

                branch = repo.Branches.Add("bar", repo.Head.Tip, null, "BAR");
                AssertRefLogEntry(repo, branch.CanonicalName, repo.Head.Tip.Id, "BAR");
            }
        }

        [Fact]
        public void MovingABranchIncludesTheCorrectReflogEntries()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                EnableRefLog(repo);
                var master = repo.Branches["master"];
                var newMaster = repo.Branches.Move(master, "new-master");
                AssertRefLogEntry(repo, newMaster.CanonicalName, newMaster.Tip.Id, 
                    "branch: renamed refs/heads/master to refs/heads/new-master");

                newMaster = repo.Branches.Move(newMaster, "new-master2", null, "MOVE");
                AssertRefLogEntry(repo, newMaster.CanonicalName, newMaster.Tip.Id, "MOVE");
            }
        }
    }
}
