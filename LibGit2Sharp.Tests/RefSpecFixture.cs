using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class RefSpecFixture : BaseFixture
    {
        [Fact]
        public void CanCountRefSpecs()
        {
            var path = SandboxStandardTestRepo();
            using (var repo = InitIsolatedRepository(path))
            {
                var remote = repo.Network.Remotes["origin"];
                Assert.Equal(1, remote.RefSpecs.Count());
            }
        }

        [Fact]
        public void CanIterateOverRefSpecs()
        {
            var path = SandboxStandardTestRepo();
            using (var repo = InitIsolatedRepository(path))
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
        public void FetchAndPushRefSpecsComposeRefSpecs()
        {
            var path = SandboxStandardTestRepo();
            using (var repo = InitIsolatedRepository(path))
            {
                var remote = repo.Network.Remotes["origin"];

                var totalRefSpecs = remote.FetchRefSpecs.Concat(remote.PushRefSpecs);
                var orderedRefSpecs = remote.RefSpecs.OrderBy(r => r.Direction == RefSpecDirection.Fetch ? 0 : 1);
                Assert.Equal(orderedRefSpecs, totalRefSpecs);
            }
        }

        [Fact]
        public void CanReadRefSpecDetails()
        {
            var path = SandboxStandardTestRepo();
            using (var repo = InitIsolatedRepository(path))
            {
                var remote = repo.Network.Remotes["origin"];

                RefSpec refSpec = remote.RefSpecs.First();
                Assert.NotNull(refSpec);

                Assert.Equal("refs/heads/*", refSpec.Source);
                Assert.Equal("refs/remotes/origin/*", refSpec.Destination);
                Assert.Equal(true, refSpec.ForceUpdate);
            }
        }

        [Theory]
        [InlineData(new string[] { "+refs/tags/*:refs/tags/*" }, new string[] { "refs/heads/*:refs/remotes/test/*", "+refs/abc:refs/def" })]
        [InlineData(new string[] { "+refs/abc/x:refs/def/x", "refs/def:refs/ghi" }, new string[0])]
        [InlineData(new string[0], new string[] { "refs/ghi:refs/jkl/mno" })]
        public void CanReplaceRefSpecs(string[] newFetchRefSpecs, string[] newPushRefSpecs)
        {
            var path = SandboxStandardTestRepo();
            using (var repo = InitIsolatedRepository(path))
            {
                var remote = repo.Network.Remotes["origin"];
                var oldRefSpecs = remote.RefSpecs.ToList();

                var newRemote = repo.Network.Remotes.Update(remote,
                    r => r.FetchRefSpecs = newFetchRefSpecs, r => r.PushRefSpecs = newPushRefSpecs);

                Assert.Equal(oldRefSpecs, remote.RefSpecs.ToList());

                var actualNewFetchRefSpecs = newRemote.RefSpecs
                    .Where(s => s.Direction == RefSpecDirection.Fetch)
                    .Select(r => r.Specification)
                    .ToArray();
                Assert.Equal(newFetchRefSpecs, actualNewFetchRefSpecs);

                var actualNewPushRefSpecs = newRemote.RefSpecs
                    .Where(s => s.Direction == RefSpecDirection.Push)
                    .Select(r => r.Specification)
                    .ToArray();
                Assert.Equal(newPushRefSpecs, actualNewPushRefSpecs);
            }
        }

        [Fact]
        public void RemoteUpdaterSavesRefSpecsPermanently()
        {
            var fetchRefSpecs = new string[] { "refs/their/heads/*:refs/my/heads/*", "+refs/their/tag:refs/my/tag" };

            var path = SandboxStandardTestRepo();
            using (var repo = InitIsolatedRepository(path))
            {
                var remote = repo.Network.Remotes["origin"];
                repo.Network.Remotes.Update(remote, r => r.FetchRefSpecs = fetchRefSpecs);
            }

            using (var repo = InitIsolatedRepository(path))
            {
                var remote = repo.Network.Remotes["origin"];
                var actualRefSpecs = remote.RefSpecs
                    .Where(r => r.Direction == RefSpecDirection.Fetch)
                    .Select(r => r.Specification)
                    .ToArray();
                Assert.Equal(fetchRefSpecs, actualRefSpecs);
            }
        }

        [Fact]
        public void CanAddAndRemoveRefSpecs()
        {
            string newRefSpec = "+refs/heads/test:refs/heads/other-test";

            var path = SandboxStandardTestRepo();
            using (var repo = InitIsolatedRepository(path))
            {
                var remote = repo.Network.Remotes["origin"];

                remote = repo.Network.Remotes.Update(remote,
                    r => r.FetchRefSpecs.Add(newRefSpec),
                    r => r.PushRefSpecs.Add(newRefSpec));

                Assert.Contains(newRefSpec, remote.FetchRefSpecs.Select(r => r.Specification));
                Assert.Contains(newRefSpec, remote.PushRefSpecs.Select(r => r.Specification));

                remote = repo.Network.Remotes.Update(remote,
                    r => r.FetchRefSpecs.Remove(newRefSpec),
                    r => r.PushRefSpecs.Remove(newRefSpec));

                Assert.DoesNotContain(newRefSpec, remote.FetchRefSpecs.Select(r => r.Specification));
                Assert.DoesNotContain(newRefSpec, remote.PushRefSpecs.Select(r => r.Specification));
            }
        }

        [Fact]
        public void CanClearRefSpecs()
        {
            var path = SandboxStandardTestRepo();
            using (var repo = InitIsolatedRepository(path))
            {
                var remote = repo.Network.Remotes["origin"];

                // Push refspec does not exist in cloned repository
                remote = repo.Network.Remotes.Update(remote, r => r.PushRefSpecs.Add("+refs/test:refs/test"));

                remote = repo.Network.Remotes.Update(remote,
                    r => r.FetchRefSpecs.Clear(),
                    r => r.PushRefSpecs.Clear());

                Assert.Empty(remote.FetchRefSpecs);
                Assert.Empty(remote.PushRefSpecs);
                Assert.Empty(remote.RefSpecs);
            }
        }

        [Theory]
        [InlineData("refs/test:refs//double-slash")]
        [InlineData("refs/trailing-slash/:refs/test")]
        [InlineData("refs/.dotfile:refs/test")]
        [InlineData("refs/.:refs/dotdir")]
        [InlineData("refs/asterix:refs/not-matching/*")]
        [InlineData("refs/double/*/asterix/*:refs/double/*asterix/*")]
        [InlineData("refs/ whitespace:refs/test")]
        public void SettingInvalidRefSpecsThrows(string refSpec)
        {
            var path = SandboxStandardTestRepo();
            using (var repo = InitIsolatedRepository(path))
            {
                var remote = repo.Network.Remotes["origin"];
                var oldRefSpecs = remote.RefSpecs.Select(r => r.Specification).ToList();

                Assert.Throws<InvalidSpecificationException>(() =>
                    repo.Network.Remotes.Update(remote, r => r.FetchRefSpecs.Add(refSpec)));

                var newRemote = repo.Network.Remotes["origin"];
                Assert.Equal(oldRefSpecs, newRemote.RefSpecs.Select(r => r.Specification).ToList());
            }
        }
    }
}
