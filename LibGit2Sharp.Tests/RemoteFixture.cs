using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class RemoteFixture : BaseFixture
    {
        [Fact]
        public void CanGetRemoteOrigin()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Remote origin = repo.Network.Remotes["origin"];
                Assert.NotNull(origin);
                AssertBelongsToARepository(repo, origin);
                Assert.Equal("origin", origin.Name);
                Assert.Equal("c:/GitHub/libgit2sharp/Resources/testrepo.git", origin.Url);
            }
        }

        [Fact]
        public void GettingRemoteThatDoesntExistReturnsNull()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Null(repo.Network.Remotes["test"]);
            }
        }

        [Fact]
        public void CanEnumerateTheRemotes()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                int count = 0;

                foreach (Remote remote in repo.Network.Remotes)
                {
                    Assert.NotNull(remote);
                    AssertBelongsToARepository(repo, remote);
                    count++;
                }

                Assert.Equal(2, count);
            }
        }

        [Theory]
        [InlineData(TagFetchMode.All)]
        [InlineData(TagFetchMode.Auto)]
        [InlineData(TagFetchMode.None)]
        public void CanSetTagFetchMode(TagFetchMode tagFetchMode)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string name = "upstream";
                const string url = "https://github.com/libgit2/libgit2sharp.git";

                repo.Network.Remotes.Add(name, url);
                Remote remote = repo.Network.Remotes[name];
                Assert.NotNull(remote);

                Remote updatedremote = repo.Network.Remotes.Update(remote,
                    r => r.TagFetchMode = tagFetchMode);

                Assert.Equal(tagFetchMode, updatedremote.TagFetchMode);
            }
        }

        [Fact]
        public void CanSetRemoteUrl()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string name = "upstream";
                const string url = "https://github.com/libgit2/libgit2sharp.git";
                const string newUrl = "https://github.com/libgit2/libgit2.git";

                repo.Network.Remotes.Add(name, url);
                Remote remote = repo.Network.Remotes[name];
                Assert.NotNull(remote);

                Remote updatedremote = repo.Network.Remotes.Update(remote,
                    r => r.Url = newUrl);

                Assert.Equal(newUrl, updatedremote.Url);
                // with no push url set, PushUrl defaults to the fetch url
                Assert.Equal(newUrl, updatedremote.PushUrl);
            }
        }

        [Fact]
        public void CanSetRemotePushUrl()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string name = "upstream";
                const string url = "https://github.com/libgit2/libgit2sharp.git";
                const string pushurl = "https://github.com/libgit2/libgit2.git";

                repo.Network.Remotes.Add(name, url);
                Remote remote = repo.Network.Remotes[name];
                Assert.NotNull(remote);

                // before setting push, both push and fetch urls should match
                Assert.Equal(url, remote.Url);
                Assert.Equal(url, remote.PushUrl);

                Remote updatedremote = repo.Network.Remotes.Update(remote,
                    r => r.PushUrl = pushurl);

                // url should not change, push url should be set to new value
                Assert.Equal(url, updatedremote.Url);
                Assert.Equal(pushurl, updatedremote.PushUrl);
            }
        }

        [Fact]
        public void CanCheckEqualityOfRemote()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Remote oneOrigin = repo.Network.Remotes["origin"];
                Assert.NotNull(oneOrigin);

                Remote otherOrigin = repo.Network.Remotes["origin"];
                Assert.Equal(oneOrigin, otherOrigin);

                Remote createdRemote = repo.Network.Remotes.Add("origin2", oneOrigin.Url);

                Remote loadedRemote = repo.Network.Remotes["origin2"];
                Assert.NotNull(loadedRemote);
                Assert.Equal(createdRemote, loadedRemote);

                Assert.NotEqual(oneOrigin, loadedRemote);
            }
        }

        [Fact]
        public void CreatingANewRemoteAddsADefaultRefSpec()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string name = "upstream";
                const string url = "https://github.com/libgit2/libgit2sharp.git";

                repo.Network.Remotes.Add(name, url);
                Remote remote = repo.Network.Remotes[name];
                Assert.NotNull(remote);

                Assert.Equal(name, remote.Name);
                Assert.Equal(url, remote.Url);

                var refSpec = repo.Config.Get<string>("remote", remote.Name, "fetch");
                Assert.NotNull(refSpec);

                Assert.Equal("+refs/heads/*:refs/remotes/upstream/*", refSpec.Value);
            }
        }

        [Fact]
        public void CanAddANewRemoteWithAFetchRefSpec()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                const string name = "pull-requests";
                const string url = "https://github.com/libgit2/libgit2sharp.git";
                const string fetchRefSpec = "+refs/pull/*:refs/remotes/pull-requests/*";

                repo.Network.Remotes.Add(name, url, fetchRefSpec);

                var refSpec = repo.Config.Get<string>("remote", name, "fetch");
                Assert.NotNull(refSpec);

                Assert.Equal(fetchRefSpec, refSpec.Value);
            }
        }

        [Theory]
        [InlineData("sher.lock")]
        [InlineData("/")]
        public void AddingARemoteWithAnInvalidNameThrows(string name)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                const string url = "https://github.com/libgit2/libgit2sharp.git";

                Assert.Throws<InvalidSpecificationException>(() => repo.Network.Remotes.Add(name, url));
            }
        }

        [Theory]
        [InlineData("valid/remote", true)]
        [InlineData("sher.lock", false)]
        [InlineData("/", false)]
        public void CanTellIfARemoteNameIsValid(string refname, bool expectedResult)
        {
            Assert.Equal(expectedResult, Remote.IsValidName(refname));
        }

        [Fact]
        public void DoesNotThrowWhenARemoteHasNoUrlSet()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                var noUrlRemote = repo.Network.Remotes["no_url"];
                Assert.NotNull(noUrlRemote);
                Assert.Equal(null, noUrlRemote.Url);

                var remotes = repo.Network.Remotes.ToList();
                Assert.Equal(1, remotes.Count(r => r.Name == "no_url"));
            }
        }

        [Fact]
        public void CreatingARemoteAddsADefaultFetchRefSpec()
        {
            var path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var remote = repo.Network.Remotes.Add("one", "http://github.com/up/stream");
                Assert.Equal("+refs/heads/*:refs/remotes/one/*", remote.RefSpecs.Single().Specification);
            }
        }

        [Fact]
        public void CanCreateARemoteWithASpecifiedFetchRefSpec()
        {
            var path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var remote = repo.Network.Remotes.Add("two", "http://github.com/up/stream", "+refs/heads/*:refs/remotes/grmpf/*");
                Assert.Equal("+refs/heads/*:refs/remotes/grmpf/*", remote.RefSpecs.Single().Specification);
            }
        }

        [Fact]
        public void CanDeleteExistingRemote()
        {
            var path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.NotNull(repo.Network.Remotes["origin"]);
                Assert.NotEmpty(repo.Refs.FromGlob("refs/remotes/origin/*"));

                repo.Network.Remotes.Remove("origin");
                Assert.Null(repo.Network.Remotes["origin"]);
                Assert.Empty(repo.Refs.FromGlob("refs/remotes/origin/*"));
            }
        }

        [Fact]
        public void CanDeleteNonExistingRemote()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Null(repo.Network.Remotes["i_dont_exist"]);
                repo.Network.Remotes.Remove("i_dont_exist");
            }
        }

        [Fact]
        public void CanRenameExistingRemote()
        {
            var path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.NotNull(repo.Network.Remotes["origin"]);
                Assert.Null(repo.Network.Remotes["renamed"]);
                Assert.NotEmpty(repo.Refs.FromGlob("refs/remotes/origin/*"));
                Assert.Empty(repo.Refs.FromGlob("refs/remotes/renamed/*"));

                repo.Network.Remotes.Rename("origin", "renamed", problem => Assert.True(false));
                Assert.Null(repo.Network.Remotes["origin"]);
                Assert.Empty(repo.Refs.FromGlob("refs/remotes/origin/*"));

                Assert.NotNull(repo.Network.Remotes["renamed"]);
                Assert.NotEmpty(repo.Refs.FromGlob("refs/remotes/renamed/*"));
            }
        }

        [Fact]
        public void RenamingNonExistingRemoteThrows()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Throws<NotFoundException>(() =>
                {
                    repo.Network.Remotes.Rename("i_dont_exist", "i_dont_either");
                });
            }
        }

        [Fact]
        public void ReportsRemotesWithNonDefaultRefSpecs()
        {
            var path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.NotNull(repo.Network.Remotes["origin"]);

                repo.Network.Remotes.Update(
                    repo.Network.Remotes["origin"],
                    r => r.FetchRefSpecs = new[] { "+refs/heads/*:refs/remotes/upstream/*" });

                repo.Network.Remotes.Rename("origin", "nondefault", problem => Assert.Equal("+refs/heads/*:refs/remotes/upstream/*", problem));

                Assert.NotEmpty(repo.Refs.FromGlob("refs/remotes/nondefault/*"));
                Assert.Empty(repo.Refs.FromGlob("refs/remotes/upstream/*"));

                Assert.Null(repo.Network.Remotes["origin"]);
                Assert.NotNull(repo.Network.Remotes["nondefault"]);
            }
        }

        [Fact]
        public void DoesNotReportRemotesWithAlreadyExistingRefSpec()
        {
            var path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.NotNull(repo.Network.Remotes["origin"]);

                repo.Refs.Add("refs/remotes/renamed/master", "32eab9cb1f450b5fe7ab663462b77d7f4b703344");

                repo.Network.Remotes.Rename("origin", "renamed", problem => Assert.True(false));

                Assert.NotEmpty(repo.Refs.FromGlob("refs/remotes/renamed/*"));
                Assert.Empty(repo.Refs.FromGlob("refs/remotes/origin/*"));

                Assert.Null(repo.Network.Remotes["origin"]);
                Assert.NotNull(repo.Network.Remotes["renamed"]);
            }
        }

        [Fact]
        public void CanNotRenameWhenRemoteWithSameNameExists()
        {
            const string name = "upstream";
            const string url = "https://github.com/libgit2/libgit2sharp.git";

            var path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.NotNull(repo.Network.Remotes["origin"]);
                repo.Network.Remotes.Add(name, url);

                Assert.Throws<NameConflictException>(() => repo.Network.Remotes.Rename("origin", "upstream"));
            }
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData(null, false, false)]
        [InlineData(null, true, true)]
        [InlineData(false, null, false)]
        [InlineData(false, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, null, true)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void ShoudlPruneOnFetchReflectsTheConfiguredSetting(bool? fetchPrune, bool? remotePrune, bool expectedFetchPrune)
        {
            var path = SandboxStandardTestRepo();
            var scd = BuildSelfCleaningDirectory();

            using (var repo = new Repository(path, BuildFakeConfigs(scd)))
            {
                Assert.Null(repo.Config.Get<bool>("fetch.prune"));
                Assert.Null(repo.Config.Get<bool>("remote.origin.prune"));

                SetIfNotNull(repo, "fetch.prune", fetchPrune);
                SetIfNotNull(repo, "remote.origin.prune", remotePrune);

                var remote = repo.Network.Remotes["origin"];
                Assert.Equal(expectedFetchPrune, remote.AutomaticallyPruneOnFetch);
            }
        }

        private void SetIfNotNull(IRepository repo, string configName, bool? value)
        {
            if (!value.HasValue)
            {
                return;
            }

            repo.Config.Set(configName, value.Value);
        }
    }
}
