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
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Remote origin = repo.Network.Remotes["origin"];
                Assert.NotNull(origin);
                Assert.Equal("origin", origin.Name);
                Assert.Equal("c:/GitHub/libgit2sharp/Resources/testrepo.git", origin.Url);
            }
        }

        [Fact]
        public void GettingRemoteThatDoesntExistReturnsNull()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Null(repo.Network.Remotes["test"]);
            }
        }

        [Fact]
        public void CanEnumerateTheRemotes()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                int count = 0;

                foreach (Remote remote in repo.Network.Remotes)
                {
                    Assert.NotNull(remote);
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
            string path = CloneBareTestRepo();
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
        public void CanCheckEqualityOfRemote()
        {
            string path = CloneStandardTestRepo();
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
            string path = CloneStandardTestRepo();
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
            string path = CloneStandardTestRepo();
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
            using (var repo = new Repository(BareTestRepoPath))
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
            using (var repo = new Repository(BareTestRepoPath))
            {
                Assert.Equal(expectedResult, repo.Network.Remotes.IsValidName(refname));
            }
        }

        [Fact]
        public void DoesNotThrowWhenARemoteHasNoUrlSet()
        {
            using (var repo = new Repository(StandardTestRepoPath))
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
            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var remote = repo.Network.Remotes.Add("one", "http://github.com/up/stream");
                Assert.Equal("+refs/heads/*:refs/remotes/one/*", remote.RefSpecs.Single().Specification);
            }
        }

        [Fact]
        public void CanCreateARemoteWithASpecifiedFetchRefSpec()
        {
            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var remote = repo.Network.Remotes.Add("two", "http://github.com/up/stream", "+refs/heads/*:refs/remotes/grmpf/*");
                Assert.Equal("+refs/heads/*:refs/remotes/grmpf/*", remote.RefSpecs.Single().Specification);
            }
        }
    }
}
