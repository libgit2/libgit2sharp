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
            using (var repo = new Repository(Constants.StandardTestRepoPath))
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
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                repo.Remotes["test"].ShouldBeNull();
            }
        }
    }
}
