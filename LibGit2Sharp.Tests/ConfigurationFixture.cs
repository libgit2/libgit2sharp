using System;
using System.IO;
using System.Text.RegularExpressions;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class ConfigurationFixture : BaseFixture
    {
        private static void AssertValueInConfigFile(string repoPath, string regex)
        {
            var configFilePath = Path.Combine(repoPath, "config");
            var text = File.ReadAllText(configFilePath);
            var r = new Regex(regex).Match(text);
            Assert.IsTrue(r.Success, text);
        }

        [Test]
        public void CanReadBooleanValue()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.IsTrue(repo.Config.Get<bool>("core.ignorecase"));
            }
        }

        [Test]
        public void CanReadIntValue()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.AreEqual(2, repo.Config.Get<int>("unittests.intsetting"));
            }
        }

        [Test]
        public void CanReadLongValue()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.AreEqual(15234, repo.Config.Get<long>("unittests.longsetting"));
            }
        }

        [Test]
        public void CanReadStringValue()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.AreEqual("+refs/heads/*:refs/remotes/origin/*", repo.Config.Get<string>("remote.origin.fetch"));
            }
        }

        [Test]
        public void CanSetBooleanValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.boolsetting", true);

                AssertValueInConfigFile(path.RepositoryPath, "boolsetting = true$");
            }
        }

        [Test]
        public void CanDeleteConfiguration()
        {
            var path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.boolsetting", true);

                repo.Config.Delete("unittests.boolsetting");
            } // config file is guaranteed to be saved when config object is freed

            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Get<bool>("unittests.boolsetting").ShouldBeFalse();
            }
        }

        [Test]
        public void CanSetIntValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.intsetting", 3);

                AssertValueInConfigFile(path.RepositoryPath, "intsetting = 3$");
            }
        }

        [Test]
        public void CanSetLongValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.longsetting", (long)451);

                AssertValueInConfigFile(path.RepositoryPath, "longsetting = 451");
            }
        }

        [Test]
        public void CanSetStringValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(Constants.StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.stringsetting", "val");

                AssertValueInConfigFile(path.RepositoryPath, "stringsetting = val$");
            }
        }

        [Test]
        public void ReadingUnsupportedTypeThrows()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Config.Get<short>("unittests.setting"));
                Assert.Throws<ArgumentException>(() => repo.Config.Get<Configuration>("unittests.setting"));
            }
        }

        [Test]
        public void ReadingValueThatDoesntExistReturnsDefault()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                repo.Config.Get<string>("unittests.ghostsetting").ShouldBeNull();
                repo.Config.Get<int>("unittests.ghostsetting").ShouldEqual(0);
                repo.Config.Get<long>("unittests.ghostsetting").ShouldEqual(0L);
                repo.Config.Get<bool>("unittests.ghostsetting").ShouldBeFalse();
                repo.Config.Get("unittests.ghostsetting", "42").ShouldEqual("42");
                repo.Config.Get("unittests.ghostsetting", 42).ShouldEqual(42);
                repo.Config.Get("unittests.ghostsetting", 42L).ShouldEqual(42L);
                repo.Config.Get("unittests.ghostsetting", true).ShouldBeTrue();
            }
        }

        [Test]
        public void SettingUnsupportedTypeThrows()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Config.Set("unittests.setting", (short)123));
                Assert.Throws<ArgumentException>(() => repo.Config.Set("unittests.setting", repo.Config));
            }
        }
    }
}
