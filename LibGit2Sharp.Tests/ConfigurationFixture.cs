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

        private static string RetrieveGlobalConfigLocation()
        {
            string[] variables = { "HOME", "USERPROFILE", };

            foreach (string variable in variables)
            {
                string potentialLocation = Environment.GetEnvironmentVariable(variable);
                if (string.IsNullOrEmpty(potentialLocation))
                {
                    continue;
                }

                string potentialPath = Path.Combine(potentialLocation, ".gitconfig");

                if (File.Exists(potentialPath))
                {
                    return potentialPath;
                }
            }

            throw new InvalidOperationException("Unable to determine the location of '.gitconfig' file.");
        }

        private static void AssertValueInGlobalConfigFile(string regex)
        {
            string configFilePath = RetrieveGlobalConfigLocation();
            AssertValueInConfigFile(configFilePath, regex);
        }

        [Test]
        public void CanDeleteConfiguration()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Get<bool>("unittests.boolsetting", false).ShouldBeFalse();

                repo.Config.Set("unittests.boolsetting", true);
                repo.Config.Get<bool>("unittests.boolsetting", false).ShouldBeTrue();

                repo.Config.Delete("unittests.boolsetting");

                repo.Config.Get<bool>("unittests.boolsetting", false).ShouldBeFalse();
            }
        }

        [Test]
        public void CanGetGlobalStringValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                InconclusiveIf(() => !repo.Config.HasGlobalConfig, "No Git global configuration available");

                repo.Config.Get<string>("user.name", null).ShouldNotBeNull();
            }
        }

        [Test]
        public void CanGetGlobalStringValueWithoutRepo()
        {
            using (var config = new Configuration())
            {
                InconclusiveIf(() => !config.HasGlobalConfig, "No Git global configuration available");
                config.Get<string>("user.name", null).ShouldNotBeNull();
            }
        }

        [Test]
        public void CanReadBooleanValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.IsTrue(repo.Config.Get<bool>("core.ignorecase", false));
                Assert.IsTrue(repo.Config.Get<bool>("core", "ignorecase", false));
            }
        }

        [Test]
        public void CanReadIntValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.AreEqual(2, repo.Config.Get<int>("unittests.intsetting", 42));
                Assert.AreEqual(2, repo.Config.Get<int>("unittests", "intsetting", 42));
            }
        }

        [Test]
        public void CanReadLongValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.AreEqual(15234, repo.Config.Get<long>("unittests.longsetting", 42));
                Assert.AreEqual(15234, repo.Config.Get<long>("unittests", "longsetting", 42));
            }
        }

        [Test]
        public void CanReadStringValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.AreEqual("+refs/heads/*:refs/remotes/origin/*", repo.Config.Get<string>("remote.origin.fetch", null));
                Assert.AreEqual("+refs/heads/*:refs/remotes/origin/*", repo.Config.Get<string>("remote", "origin", "fetch", null));
            }
        }

        [Test]
        public void CanSetBooleanValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.boolsetting", true);

                AssertValueInLocalConfigFile(path.RepositoryPath, "boolsetting = true$");
            }
        }

        [Test]
        public void CanSetGlobalStringValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                InconclusiveIf(() => !repo.Config.HasGlobalConfig, "No Git global configuration available");

                var existing = repo.Config.Get<string>("user.name", null);
                existing.ShouldNotBeNull();

                try
                {
                    repo.Config.Set("user.name", "Unit Test", ConfigurationLevel.Global);

                    AssertValueInGlobalConfigFile("name = Unit Test$");
                }
                finally
                {
                    repo.Config.Set("user.name", existing, ConfigurationLevel.Global);
                }
            }
        }

        [Test]
        public void CanSetGlobalStringValueWithoutRepo()
        {
            using(var config = new Configuration())
            {
                InconclusiveIf(() => !config.HasGlobalConfig, "No Git global configuration available");

                var existing = config.Get<string>("user.name", null);
                existing.ShouldNotBeNull();

                try
                {
                    config.Set("user.name", "Unit Test", ConfigurationLevel.Global);

                    AssertValueInGlobalConfigFile("name = Unit Test$");
                }
                finally
                {
                    config.Set("user.name", existing, ConfigurationLevel.Global);
                }
            }
        }

        [Test]
        public void SettingLocalConfigurationOutsideAReposThrows()
        {
            using (var config = new Configuration())
            {
                Assert.Throws<LibGit2Exception>(() => config.Set("unittests.intsetting", 3));
            }
        }

        [Test]
        public void CanSetIntValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.intsetting", 3);

                AssertValueInLocalConfigFile(path.RepositoryPath, "intsetting = 3$");
            }
        }

        [Test]
        public void CanSetLongValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.longsetting", (long)451);

                AssertValueInLocalConfigFile(path.RepositoryPath, "longsetting = 451");
            }
        }

        [Test]
        public void CanSetStringValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.stringsetting", "val");

                AssertValueInLocalConfigFile(path.RepositoryPath, "stringsetting = val$");
            }
        }

        [Test]
        public void ReadingUnsupportedTypeThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Config.Get<short>("unittests.setting", 42));
                Assert.Throws<ArgumentException>(() => repo.Config.Get<Configuration>("unittests.setting", null));
            }
        }

        [Test]
        public void ReadingValueThatDoesntExistReturnsDefault()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                repo.Config.Get<string>("unittests.ghostsetting", null).ShouldBeNull();
                repo.Config.Get<int>("unittests.ghostsetting", 0).ShouldEqual(0);
                repo.Config.Get<long>("unittests.ghostsetting", 0L).ShouldEqual(0L);
                repo.Config.Get<bool>("unittests.ghostsetting", false).ShouldBeFalse();
                repo.Config.Get("unittests.ghostsetting", "42").ShouldEqual("42");
                repo.Config.Get("unittests.ghostsetting", 42).ShouldEqual(42);
                repo.Config.Get("unittests.ghostsetting", 42L).ShouldEqual(42L);
                repo.Config.Get("unittests.ghostsetting", true).ShouldBeTrue();
            }
        }

        [Test]
        public void SettingUnsupportedTypeThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Config.Set("unittests.setting", (short)123));
                Assert.Throws<ArgumentException>(() => repo.Config.Set("unittests.setting", repo.Config));
            }
        }
    }
}
