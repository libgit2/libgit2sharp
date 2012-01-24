using LibGit2Sharp.Core;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;
using System.Linq;

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

        [TestCase("http://github.com/nulltoken/TestGitRepository.git")]
        [TestCase("git://github.com/nulltoken/TestGitRepository.git")]
        public void AwesomeTest(string remoteLocation)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            string dir = Repository.Init(scd.DirectoryPath, true);

            using (var repo = new Repository(dir))
            {
                repo.Branches.Count().ShouldEqual(0);

                repo.Config.Set("remote.origin.fetch", "+refs/heads/*:refs/remotes/origin/*");
                repo.Config.Set("remote.origin.url", remoteLocation);

				repo.Fetch("origin");
				// Now, how do I test that everything's OK?

                repo.Branches.Count().ShouldNotEqual(0);
            }

            //Assert.IsNotNullOrEmpty(a);
        }
    }
}
