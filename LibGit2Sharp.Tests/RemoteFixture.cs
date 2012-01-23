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

        [Test]
        public void AwesomeTest()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            string dir = Repository.Init(scd.DirectoryPath, true);

            using (var repo = new Repository(dir))
            {
				repo.Fetch("http://github.com/libgit2/libgit2.git");
				// Now, how do I test that everything's OK?
            }

            //Assert.IsNotNullOrEmpty(a);
        }
    }
}
