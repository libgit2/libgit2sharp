using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class RemoteFixture : BaseFixture
    {
        [Test]
        public void CanGetRemoteOrigin()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                var origin = repo.Remotes["origin"];
                origin.ShouldNotBeNull();
                origin.Name.ShouldEqual("origin");
                origin.Url.ShouldEqual("c:/GitHub/libgit2sharp/Resources/testrepo.git");
            }
        }

        [Test]
        public void GettingRemoteThatDoesntExistThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                repo.Remotes["test"].ShouldBeNull();
            }
        }

        [Test]
        public void CanCheckEqualityOfRemote()
        {
            TemporaryCloneOfTestRepo path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);

            using (var repo = new Repository(path.RepositoryPath))
            {
                Remote oneOrigin = repo.Remotes["origin"];
                oneOrigin.ShouldNotBeNull();

                Remote otherOrigin = repo.Remotes["origin"];
                otherOrigin.ShouldEqual(oneOrigin);

                repo.Config.Set("remote.origin2.url", oneOrigin.Url);

                /* LibGit2 cringes when a remote doesn't expose a fetch refspec */
                var originFetch = repo.Config.Get<string>("remote", "origin", "fetch", null);
                repo.Config.Set("remote.origin2.fetch", originFetch);

                Remote differentRemote = repo.Remotes["origin2"];
                differentRemote.ShouldNotBeNull();

                differentRemote.ShouldNotEqual(oneOrigin);
            }
        }
    }
}
