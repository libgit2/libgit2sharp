﻿using System.Linq;
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
            Assert.Equal(expectedResult, Remote.IsValidName(refname));
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

        [Fact]
        public void CanDeleteExistingRemote()
        {
            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.NotNull(repo.Network.Remotes["origin"]);
                Assert.NotEmpty(repo.Refs.FromGlob("refs/remotes/origin/*"));

                repo.Network.Remotes.Remove("origin");
                Assert.Null(repo.Network.Remotes["origin"]);
                Assert.Empty(repo.Refs.FromGlob("refs/remotes/origin/*"));
            }
        }

        [Fact]
        public void CanDeleteNonExistingRemote()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Null(repo.Network.Remotes["i_dont_exist"]);
                repo.Network.Remotes.Remove("i_dont_exist");
            }
        }

        [Fact]
        public void CanRenameExistingRemote()
        {
            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.NotNull(repo.Network.Remotes["origin"]);
                Assert.Null(repo.Network.Remotes["renamed"]);
                Assert.NotEmpty(repo.Refs.FromGlob("refs/remotes/origin/*"));
                Assert.Empty(repo.Refs.FromGlob("refs/remotes/renamed/*"));

                repo.Network.Remotes.Rename("origin", "renamed", problem => Assert.True(false));
                Assert.Null(repo.Network.Remotes["origin"]);
                Assert.Empty(repo.Refs.FromGlob("refs/remotes/origin/*"));

                Assert.NotNull(repo.Network.Remotes["renamed"]);
                Assert.NotEmpty(repo.Refs.FromGlob("refs/remotes/renamed/*"));
            }
        }

        [Fact]
        public void CanRenameNonExistingRemote()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Null(repo.Network.Remotes["i_dont_exist"]);

                repo.Network.Remotes.Rename("i_dont_exist", "i_dont_either", problem => Assert.True(false));
                Assert.Null(repo.Network.Remotes["i_dont_either"]);
            }
        }

        [Fact]
        public void ReportsRemotesWithNonDefaultRefSpecs()
        {
            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.NotNull(repo.Network.Remotes["origin"]);

                repo.Network.Remotes.Update(
                    repo.Network.Remotes["origin"],
                    r => r.FetchRefSpecs = new[] { "+refs/heads/*:refs/remotes/upstream/*" });

                repo.Network.Remotes.Rename("origin", "nondefault", problem => Assert.Equal("+refs/heads/*:refs/remotes/upstream/*", problem));

                Assert.NotEmpty(repo.Refs.FromGlob("refs/remotes/nondefault/*"));
                Assert.Empty(repo.Refs.FromGlob("refs/remotes/upstream/*"));

                Assert.Null(repo.Network.Remotes["origin"]);
                Assert.NotNull(repo.Network.Remotes["nondefault"]);
            }
        }

        [Fact]
        public void DoesNotReportRemotesWithAlreadyExistingRefSpec()
        {
            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.NotNull(repo.Network.Remotes["origin"]);

                repo.Refs.Add("refs/remotes/renamed/master", "32eab9cb1f450b5fe7ab663462b77d7f4b703344");

                repo.Network.Remotes.Rename("origin", "renamed", problem => Assert.True(false));

                Assert.NotEmpty(repo.Refs.FromGlob("refs/remotes/renamed/*"));
                Assert.Empty(repo.Refs.FromGlob("refs/remotes/origin/*"));

                Assert.Null(repo.Network.Remotes["origin"]);
                Assert.NotNull(repo.Network.Remotes["renamed"]);
            }
        }

        [Fact]
        public void CanNotRenameWhenRemoteWithSameNameExists()
        {
            const string name = "upstream";
            const string url = "https://github.com/libgit2/libgit2sharp.git";

            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.NotNull(repo.Network.Remotes["origin"]);
                repo.Network.Remotes.Add(name, url);

                Assert.Throws<NameConflictException>(() => repo.Network.Remotes.Rename("origin", "upstream"));
            }
        }

        [Theory]
        [InlineData("git@github.com:org/repo.git", false)]
        [InlineData("git://site.com:80/org/repo.git", true)]
        [InlineData("ssh://user@host.com:80/org/repo.git", false)]
        [InlineData("http://site.com:80/org/repo.git", true)]
        [InlineData("https://github.com:80/org/repo.git", true)]
        [InlineData("ftp://site.com:80/org/repo.git", false)]
        [InlineData("ftps://site.com:80/org/repo.git", false)]
        [InlineData("file:///path/repo.git", true)]
        [InlineData("protocol://blah.meh/whatever.git", false)]
        public void CanCheckIfUrlisSupported(string url, bool supported)
        {
            Assert.Equal(supported, Remote.IsSupportedUrl(url));
        }
    }
}
