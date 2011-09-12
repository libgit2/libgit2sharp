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
        private static void AssertValueInLocalConfigFile(string repoPath, string regex)
        {
            var configFilePath = Path.Combine(repoPath, "config");
            AssertValueInConfigFile(configFilePath, regex);
        }

        private static void AssertValueInConfigFile(string configFilePath, string regex)
        {
            var text = File.ReadAllText(configFilePath);
            var r = new Regex(regex, RegexOptions.Multiline).Match(text);
            Assert.IsTrue(r.Success, text);
        }

        private static void AssertValueInGlobalConfigFile(string regex)
        {
            var configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "../.gitconfig");
            AssertValueInConfigFile(configFilePath, regex);
        }

        private const string globalConfigFixture = "Resources/.gitconfig";

        [Test]
        public void CanDeleteConfiguration()
        {
            using (var path = new TemporaryCloneOfTestRepo(Constants.StandardTestRepoPath))
            {
                using (var repo = new Repository(path.RepositoryPath))
                {
                    repo.Config.Set("unittests.boolsetting", true);

                    repo.Config.Delete("unittests.boolsetting");
                    repo.Config.Save();

                    Assert.Throws<ApplicationException>(() => repo.Config.Get<bool>("unittests.boolsetting"));
                }
            }
        }

        [Test]
        public void CanGetGlobalStringValue()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            using (var config = new Configuration(repo, globalConfigFixture))
            {
                Assert.AreEqual("Tim Clem", config.Get<string>("user.name"));
            }
        }

        [Test]
        public void CanReadBooleanValue()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.IsFalse(repo.Config.Get<bool>("core.bare"));
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
                Assert.AreEqual("+refs/heads/*:refs/remotes/origin/*", repo.Config.Get<string>("remotes.origin.fetch"));
            }
        }

        [Test]
        public void CanSetBooleanValue()
        {
            using (var path = new TemporaryCloneOfTestRepo(Constants.StandardTestRepoPath))
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.boolsetting", true);

                AssertValueInLocalConfigFile(path.RepositoryPath, "boolsetting = true$");
            }
        }

        [Test]
        public void CanSetGlobalStringValue()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            using (var config = new Configuration(repo, globalConfigFixture))
            {
                var existing = config.Get<string>("user.name");
                try
                {
                    config.Set("user.name", "Unit Test", ConfigurationLevel.Global);
                    config.Save();

                    AssertValueInGlobalConfigFile("name = Unit Test$");
                }
                finally
                {
                    config.Set("user.name", existing, ConfigurationLevel.Global);
                }
            }
        }

        [Test]
        public void CanSetIntValue()
        {
            using (var path = new TemporaryCloneOfTestRepo(Constants.StandardTestRepoPath))
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.intsetting", 3);

                AssertValueInLocalConfigFile(path.RepositoryPath, "intsetting = 3$");
            }
        }

        [Test]
        public void CanSetLongValue()
        {
            using (var path = new TemporaryCloneOfTestRepo(Constants.StandardTestRepoPath))
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.longsetting", (long) 451);

                AssertValueInLocalConfigFile(path.RepositoryPath, "longsetting = 451");
            }
        }

        [Test]
        public void CanSetStringValue()
        {
            using (var path = new TemporaryCloneOfTestRepo(Constants.StandardTestRepoPath))
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.stringsetting", "val");

                AssertValueInLocalConfigFile(path.RepositoryPath, "stringsetting = val$");
            }
        }

        [Test]
        public void ReadingValueThatDoesntExistThrows()
        {
            using (var repo = new Repository(Constants.StandardTestRepoPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Config.Get<string>("unittests.ghostsetting"));
                Assert.Throws<ApplicationException>(() => repo.Config.Get<int>("unittests.ghostsetting"));
                Assert.Throws<ApplicationException>(() => repo.Config.Get<long>("unittests.ghostsetting"));
                Assert.Throws<ApplicationException>(() => repo.Config.Get<bool>("unittests.ghostsetting"));
            }
        }
    }
}