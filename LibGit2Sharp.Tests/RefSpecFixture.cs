using System;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class RefSpecFixture : BaseFixture
    {
        [Fact]
        public void CanCountRefSpecs()
        {
            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var remote = repo.Network.Remotes["origin"];
                Assert.Equal(1, remote.RefSpecs.Count());
            }
        }

        [Fact]
        public void CanIterateOverRefSpecs()
        {
            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var remote = repo.Network.Remotes["origin"];
                int count = 0;
                foreach (RefSpec refSpec in remote.RefSpecs)
                {
                    Assert.NotNull(refSpec);
                    count++;
                }
                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void CanReadRefSpecDetails()
        {
            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var remote = repo.Network.Remotes["origin"];

                RefSpec refSpec = remote.RefSpecs.First();
                Assert.NotNull(refSpec);

                Assert.Equal("refs/heads/*", refSpec.Source);
                Assert.Equal("refs/remotes/origin/*", refSpec.Destination);
                Assert.Equal(true, refSpec.ForceUpdate);
            }
        }
    }
}
