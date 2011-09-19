using System;
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
                Assert.IsNotNull(origin);
                Assert.AreEqual("origin", origin.Name);
                Assert.AreEqual("c:/GitHub/libgit2sharp/Resources/testrepo.git", origin.Url);
            }
        }

        [Test]
        public void GettingRemoteThatDoesntExistThrows()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.Throws<ApplicationException>(() => { var r = repo.Remotes["test"]; });
            }
        }
    }
}