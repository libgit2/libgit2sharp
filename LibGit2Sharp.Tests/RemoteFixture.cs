using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class RemoteFixture : BaseFixture
    {
        [Fact]
        public void CanGetRemoteOrigin()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Remote origin = repo.Remotes["origin"];
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
                Assert.Null(repo.Remotes["test"]);
            }
        }

        [Fact]
        public void CanEnumerateTheRemotes()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                int count = 0;

                foreach (Remote remote in repo.Remotes)
                {
                    Assert.NotNull(remote);
                    count++;
                }

                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void CanCheckEqualityOfRemote()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);

            using (var repo = new Repository(path.RepositoryPath))
            {
                Remote oneOrigin = repo.Remotes["origin"];
                Assert.NotNull(oneOrigin);

                Remote otherOrigin = repo.Remotes["origin"];
                Assert.Equal(oneOrigin, otherOrigin);

                Remote createdRemote = repo.Remotes.Add("origin2", oneOrigin.Url);

                Remote loadedRemote = repo.Remotes["origin2"];
                Assert.NotNull(loadedRemote);
                Assert.Equal(createdRemote, loadedRemote);

                Assert.NotEqual(oneOrigin, loadedRemote);
            }
        }

        [Fact]
        public void CreatingANewRemoteAddsADefaultRefSpec()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);

            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "upstream";
                const string url = "https://github.com/libgit2/libgit2sharp.git";

                repo.Remotes.Add(name, url);
                Remote remote = repo.Remotes[name];
                Assert.NotNull(remote);

                Assert.Equal(name, remote.Name);
                Assert.Equal(url, remote.Url);

                var refSpec = repo.Config.Get<string>("remote", remote.Name, "fetch", null);
                Assert.NotNull(refSpec);

                Assert.Equal("+refs/heads/*:refs/remotes/upstream/*", refSpec);
            }
        }

        [Fact]
        public void CanAddANewRemoteWithAFetchRefSpec()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);

            using (var repo = new Repository(path.RepositoryPath))
            {
                const string name = "pull-requests";
                const string url = "https://github.com/libgit2/libgit2sharp.git";
                const string fetchRefSpec = "+refs/pull/*:refs/remotes/pull-requests/*";

                repo.Remotes.Add(name, url, fetchRefSpec);

                var refSpec = repo.Config.Get<string>("remote", name, "fetch", null);
                Assert.NotNull(refSpec);

                Assert.Equal(fetchRefSpec, refSpec);
            }
        }
    }
}
