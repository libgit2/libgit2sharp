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

                Remote createdRemote = repo.Remotes.Create("origin2", oneOrigin.Url);

                Remote loadedRemote = repo.Remotes["origin2"];
                Assert.NotNull(loadedRemote);
                Assert.Equal(createdRemote, loadedRemote);

                loadedRemote.ShouldNotEqual(oneOrigin);
            }
        }
    }
}
