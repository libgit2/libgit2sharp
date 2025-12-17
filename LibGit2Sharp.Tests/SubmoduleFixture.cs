using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class SubmoduleFixture : BaseFixture
    {
        [Fact]
        public void RetrievingSubmoduleForNormalDirectoryReturnsNull()
        {
            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules["just_a_dir"];
                Assert.Null(submodule);
            }
        }

        [Fact]
        public void RetrievingSubmoduleInBranchShouldWork()
        {
            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules["sm_branch_only"];
                Assert.Null(submodule);

                Commands.Checkout(repo, "dev", new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
                submodule = repo.Submodules["sm_branch_only"];
                Assert.NotNull(submodule);
                Assert.NotEqual(SubmoduleStatus.Unmodified, submodule.RetrieveStatus());

                Commands.Checkout(repo, "master", new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
                submodule = repo.Submodules["sm_branch_only"];
                Assert.Null(submodule);
            }
        }

        [Theory]
        [InlineData("sm_added_and_uncommited", SubmoduleStatus.InConfig | SubmoduleStatus.InIndex | SubmoduleStatus.InWorkDir | SubmoduleStatus.IndexAdded)]
        [InlineData("sm_changed_file", SubmoduleStatus.InConfig | SubmoduleStatus.InHead | SubmoduleStatus.InIndex | SubmoduleStatus.InWorkDir | SubmoduleStatus.WorkDirFilesModified)]
        [InlineData("sm_changed_head", SubmoduleStatus.InConfig | SubmoduleStatus.InHead | SubmoduleStatus.InIndex | SubmoduleStatus.InWorkDir | SubmoduleStatus.WorkDirModified)]
        [InlineData("sm_changed_index", SubmoduleStatus.InConfig | SubmoduleStatus.InHead | SubmoduleStatus.InIndex | SubmoduleStatus.InWorkDir | SubmoduleStatus.WorkDirFilesIndexDirty)]
        [InlineData("sm_changed_untracked_file", SubmoduleStatus.InConfig | SubmoduleStatus.InHead | SubmoduleStatus.InIndex | SubmoduleStatus.InWorkDir | SubmoduleStatus.WorkDirFilesUntracked)]
        [InlineData("sm_gitmodules_only", SubmoduleStatus.InConfig)]
        [InlineData("sm_missing_commits", SubmoduleStatus.InConfig | SubmoduleStatus.InHead | SubmoduleStatus.InIndex | SubmoduleStatus.InWorkDir | SubmoduleStatus.WorkDirModified)]
        [InlineData("sm_unchanged", SubmoduleStatus.InConfig | SubmoduleStatus.InHead | SubmoduleStatus.InIndex | SubmoduleStatus.InWorkDir)]
        [InlineData("sm_branch_only", null)]
        public void CanRetrieveTheStatusOfASubmodule(string name, SubmoduleStatus? expectedStatus)
        {
            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules[name];

                if (expectedStatus == null)
                {
                    Assert.Null(submodule);
                    return;
                }

                Assert.NotNull(submodule);
                Assert.Equal(name, submodule.Name);
                Assert.Equal(name, submodule.Path);

                var status = submodule.RetrieveStatus();
                Assert.Equal(expectedStatus, status);
            }
        }

        [Theory]
        [InlineData("sm_added_and_uncommited", null, "480095882d281ed676fe5b863569520e54a7d5c0", "480095882d281ed676fe5b863569520e54a7d5c0")]
        [InlineData("sm_changed_file", "480095882d281ed676fe5b863569520e54a7d5c0", "480095882d281ed676fe5b863569520e54a7d5c0", "480095882d281ed676fe5b863569520e54a7d5c0")]
        [InlineData("sm_changed_head", "480095882d281ed676fe5b863569520e54a7d5c0", "480095882d281ed676fe5b863569520e54a7d5c0", "3d9386c507f6b093471a3e324085657a3c2b4247")]
        [InlineData("sm_changed_index", "480095882d281ed676fe5b863569520e54a7d5c0", "480095882d281ed676fe5b863569520e54a7d5c0", "480095882d281ed676fe5b863569520e54a7d5c0")]
        [InlineData("sm_changed_untracked_file", "480095882d281ed676fe5b863569520e54a7d5c0", "480095882d281ed676fe5b863569520e54a7d5c0", "480095882d281ed676fe5b863569520e54a7d5c0")]
        [InlineData("sm_gitmodules_only", null, null, null)]
        [InlineData("sm_missing_commits", "480095882d281ed676fe5b863569520e54a7d5c0", "480095882d281ed676fe5b863569520e54a7d5c0", "5e4963595a9774b90524d35a807169049de8ccad")]
        [InlineData("sm_unchanged", "480095882d281ed676fe5b863569520e54a7d5c0", "480095882d281ed676fe5b863569520e54a7d5c0", "480095882d281ed676fe5b863569520e54a7d5c0")]
        public void CanRetrieveTheCommitIdsOfASubmodule(string name, string headId, string indexId, string workDirId)
        {
            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules[name];
                Assert.NotNull(submodule);
                AssertBelongsToARepository(repo, submodule);
                Assert.Equal(name, submodule.Name);

                Assert.Equal((ObjectId)headId, submodule.HeadCommitId);
                Assert.Equal((ObjectId)indexId, submodule.IndexCommitId);
                Assert.Equal((ObjectId)workDirId, submodule.WorkDirCommitId);

                AssertEntryId((ObjectId)headId, repo.Head[name], c => c.Target.Id);
                AssertEntryId((ObjectId)indexId, repo.Index[name], i => i.Id);
            }
        }

        private static void AssertEntryId<T>(ObjectId expected, T entry, Func<T, ObjectId> selector)
        {
            Assert.Equal(expected, ReferenceEquals(entry, null) ? null : selector(entry));
        }

        [Fact]
        public void CanEnumerateRepositorySubmodules()
        {
            var expectedSubmodules = new[]
            {
                "sm_added_and_uncommited",
                "sm_changed_file",
                "sm_changed_head",
                "sm_changed_index",
                "sm_changed_untracked_file",
                "sm_gitmodules_only",
                "sm_missing_commits",
                "sm_unchanged",
            };

            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                var submodules = repo.Submodules.OrderBy(s => s.Name, StringComparer.Ordinal);

                Assert.Equal(expectedSubmodules, submodules.Select(s => s.Name).ToArray());
                Assert.Equal(expectedSubmodules, submodules.Select(s => s.Path).ToArray());
                Assert.Equal(Enumerable.Repeat("../submodule_target_wd", expectedSubmodules.Length).ToArray(),
                             submodules.Select(s => s.Url).ToArray());
            }
        }

        [Theory]
        [InlineData("sm_changed_head")]
        [InlineData("sm_changed_head/")]
        public void CanStageChangeInSubmoduleViaIndexStage(string submodulePath)
        {
            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules[submodulePath];
                Assert.NotNull(submodule);

                var statusBefore = submodule.RetrieveStatus();
                Assert.Equal(SubmoduleStatus.WorkDirModified, statusBefore & SubmoduleStatus.WorkDirModified);

                Commands.Stage(repo, submodulePath);

                var statusAfter = submodule.RetrieveStatus();
                Assert.Equal(SubmoduleStatus.IndexModified, statusAfter & SubmoduleStatus.IndexModified);
            }
        }

        [Theory]
        [InlineData("sm_changed_head")]
        [InlineData("sm_changed_head/")]
        public void CanStageChangeInSubmoduleViaIndexStageWithOtherPaths(string submodulePath)
        {
            var path = SandboxSubmoduleTestRepo();
            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules[submodulePath];
                Assert.NotNull(submodule);

                var statusBefore = submodule.RetrieveStatus();
                Assert.Equal(SubmoduleStatus.WorkDirModified, statusBefore & SubmoduleStatus.WorkDirModified);

                Touch(repo.Info.WorkingDirectory, "new-file.txt");

                Commands.Stage(repo, new[] { "new-file.txt", submodulePath, "does-not-exist.txt" });

                var statusAfter = submodule.RetrieveStatus();
                Assert.Equal(SubmoduleStatus.IndexModified, statusAfter & SubmoduleStatus.IndexModified);
            }
        }

        [Fact]
        public void CanInitSubmodule()
        {
            var path = SandboxSubmoduleSmallTestRepo();
            string submoduleName = "submodule_target_wd";
            string expectedSubmodulePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path), submoduleName));
            string expectedSubmoduleUrl = expectedSubmodulePath.Replace('\\', '/');

            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules[submoduleName];

                Assert.NotNull(submodule);
                Assert.True(submodule.RetrieveStatus().HasFlag(SubmoduleStatus.WorkDirUninitialized));

                var configEntryBeforeInit = repo.Config.Get<string>(string.Format("submodule.{0}.url", submoduleName));
                Assert.Null(configEntryBeforeInit);

                repo.Submodules.Init(submodule.Name, false);

                var configEntryAfterInit = repo.Config.Get<string>(string.Format("submodule.{0}.url", submoduleName));
                Assert.NotNull(configEntryAfterInit);
                Assert.Equal(expectedSubmoduleUrl, configEntryAfterInit.Value);
            }
        }

        [Fact]
        public void UpdatingUninitializedSubmoduleThrows()
        {
            var path = SandboxSubmoduleSmallTestRepo();
            string submoduleName = "submodule_target_wd";

            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules[submoduleName];

                Assert.NotNull(submodule);
                Assert.True(submodule.RetrieveStatus().HasFlag(SubmoduleStatus.WorkDirUninitialized));

                Assert.Throws<LibGit2SharpException>(() => repo.Submodules.Update(submodule.Name, new SubmoduleUpdateOptions()));
            }
        }

        [Fact]
        public void CanUpdateSubmodule()
        {
            var path = SandboxSubmoduleSmallTestRepo();
            string submoduleName = "submodule_target_wd";

            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules[submoduleName];

                Assert.NotNull(submodule);
                Assert.True(submodule.RetrieveStatus().HasFlag(SubmoduleStatus.WorkDirUninitialized));

                bool checkoutProgressCalled = false;
                bool checkoutNotifyCalled = false;
                bool updateTipsCalled = false;
                var options = new SubmoduleUpdateOptions()
                {
                    OnCheckoutProgress = (x, y, z) => checkoutProgressCalled = true,
                    OnCheckoutNotify = (x, y) => { checkoutNotifyCalled = true; return true; },
                    CheckoutNotifyFlags = CheckoutNotifyFlags.Updated,
                };

                options.FetchOptions.OnUpdateTips = (x, y, z) => { updateTipsCalled = true; return true; };

                repo.Submodules.Init(submodule.Name, false);
                repo.Submodules.Update(submodule.Name, options);

                Assert.True(submodule.RetrieveStatus().HasFlag(SubmoduleStatus.InWorkDir));
                Assert.True(checkoutProgressCalled);
                Assert.True(checkoutNotifyCalled);
                Assert.True(updateTipsCalled);
                Assert.Equal((ObjectId)"480095882d281ed676fe5b863569520e54a7d5c0", submodule.HeadCommitId);
                Assert.Equal((ObjectId)"480095882d281ed676fe5b863569520e54a7d5c0", submodule.IndexCommitId);
                Assert.Equal((ObjectId)"480095882d281ed676fe5b863569520e54a7d5c0", submodule.WorkDirCommitId);
            }
        }

        [Fact]
        public void CanInitializeAndUpdateSubmodule()
        {
            var path = SandboxSubmoduleSmallTestRepo();
            string submoduleName = "submodule_target_wd";

            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules[submoduleName];

                Assert.NotNull(submodule);
                Assert.True(submodule.RetrieveStatus().HasFlag(SubmoduleStatus.WorkDirUninitialized));

                repo.Submodules.Update(submodule.Name, new SubmoduleUpdateOptions() { Init = true });

                Assert.True(submodule.RetrieveStatus().HasFlag(SubmoduleStatus.InWorkDir));
                Assert.Equal((ObjectId)"480095882d281ed676fe5b863569520e54a7d5c0", submodule.HeadCommitId);
                Assert.Equal((ObjectId)"480095882d281ed676fe5b863569520e54a7d5c0", submodule.IndexCommitId);
                Assert.Equal((ObjectId)"480095882d281ed676fe5b863569520e54a7d5c0", submodule.WorkDirCommitId);
            }
        }

        [Fact]
        public void CanUpdateSubmoduleAfterCheckout()
        {
            var path = SandboxSubmoduleSmallTestRepo();
            string submoduleName = "submodule_target_wd";

            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules[submoduleName];

                Assert.NotNull(submodule);
                Assert.True(submodule.RetrieveStatus().HasFlag(SubmoduleStatus.WorkDirUninitialized));

                repo.Submodules.Init(submodule.Name, false);
                repo.Submodules.Update(submodule.Name, new SubmoduleUpdateOptions());

                Assert.True(submodule.RetrieveStatus().HasFlag(SubmoduleStatus.InWorkDir));

                Commands.Checkout(repo, "alternate");
                Assert.True(submodule.RetrieveStatus().HasFlag(SubmoduleStatus.WorkDirModified));

                submodule = repo.Submodules[submoduleName];

                Assert.Equal((ObjectId)"5e4963595a9774b90524d35a807169049de8ccad", submodule.HeadCommitId);
                Assert.Equal((ObjectId)"5e4963595a9774b90524d35a807169049de8ccad", submodule.IndexCommitId);
                Assert.Equal((ObjectId)"480095882d281ed676fe5b863569520e54a7d5c0", submodule.WorkDirCommitId);

                repo.Submodules.Update(submodule.Name, new SubmoduleUpdateOptions());
                submodule = repo.Submodules[submoduleName];

                Assert.Equal((ObjectId)"5e4963595a9774b90524d35a807169049de8ccad", submodule.HeadCommitId);
                Assert.Equal((ObjectId)"5e4963595a9774b90524d35a807169049de8ccad", submodule.IndexCommitId);
                Assert.Equal((ObjectId)"5e4963595a9774b90524d35a807169049de8ccad", submodule.WorkDirCommitId);
            }
        }

        [Fact]
        public void CanReadSubmoduleProperties()
        {
            var path = SandboxSubmoduleSmallTestRepo();
            string submoduleName = "submodule_target_wd";

            using (var repo = new Repository(path))
            {
                var submodule = repo.Submodules[submoduleName];

                Assert.Equal(SubmoduleUpdate.Checkout, submodule.UpdateRule);
                Assert.Equal(SubmoduleIgnore.None, submodule.IgnoreRule);

                // Libgit2 currently returns No by default, which seems incorrect -
                // I would expect OnDemand. For now, just test that we can query
                // lg2 for this property.
                Assert.Equal(SubmoduleRecurse.No, submodule.FetchRecurseSubmodulesRule);
            }
        }
    }
}
