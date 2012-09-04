using System;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class ConfigurationFixture : BaseFixture
    {
        private static void AssertValueInLocalConfigFile(string repoPath, string regex)
        {
            var configFilePath = Path.Combine(repoPath, "config");
            AssertValueInConfigFile(configFilePath, regex);
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

        [Fact]
        public void CanUnsetAnEntryFromTheLocalConfiguration()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                Assert.False(repo.Config.Get<bool>("unittests.boolsetting", false));

                repo.Config.Set("unittests.boolsetting", true);
                Assert.True(repo.Config.Get<bool>("unittests.boolsetting", false));

                repo.Config.Unset("unittests.boolsetting");

                Assert.False(repo.Config.Get<bool>("unittests.boolsetting", false));
            }
        }

        [Fact]
        public void CanUnsetAnEntryFromTheGlobalConfiguration()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            string confs = Path.Combine(scd.DirectoryPath, "confs");
            Directory.CreateDirectory(confs);

            string globalLocation = Path.Combine(confs, "my-global-config");
            string systemLocation = Path.Combine(confs, "my-system-config");

            StringBuilder sb = new StringBuilder()
                .AppendLine("[Wow]")
                .AppendFormat("Man-I-am-totally-global = 42{0}", Environment.NewLine);

            File.WriteAllText(globalLocation, sb.ToString());

            var options = new RepositoryOptions
            {
                GlobalConfigurationLocation = globalLocation,
                SystemConfigurationLocation = systemLocation,
            };

            using (var repo = new Repository(BareTestRepoPath, options))
            {
                Assert.True(repo.Config.HasGlobalConfig);
                Assert.Equal(42, repo.Config.Get("Wow.Man-I-am-totally-global", 1337));

                repo.Config.Unset("Wow.Man-I-am-totally-global");
                Assert.Equal(42, repo.Config.Get("Wow.Man-I-am-totally-global", 1337));

                repo.Config.Unset("Wow.Man-I-am-totally-global", ConfigurationLevel.Global);
                Assert.Equal(1337, repo.Config.Get("Wow.Man-I-am-totally-global", 1337));
            }
        }

        [SkippableFact]
        public void CanGetGlobalStringValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                InconclusiveIf(() => !repo.Config.HasGlobalConfig, "No Git global configuration available");

                Assert.NotNull(repo.Config.Get<string>("user.name", null));
            }
        }

        [SkippableFact]
        public void CanGetGlobalStringValueWithoutRepo()
        {
            using (var config = new Configuration())
            {
                InconclusiveIf(() => !config.HasGlobalConfig, "No Git global configuration available");
                Assert.NotNull(config.Get<string>("user.name", null));
            }
        }

        [Fact]
        public void CanReadBooleanValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.True(repo.Config.Get<bool>("core.ignorecase", false));
                Assert.True(repo.Config.Get<bool>("core", "ignorecase", false));
            }
        }

        [Fact]
        public void CanReadIntValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Equal(2, repo.Config.Get<int>("unittests.intsetting", 42));
                Assert.Equal(2, repo.Config.Get<int>("unittests", "intsetting", 42));
            }
        }

        [Fact]
        public void CanReadLongValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Equal(15234, repo.Config.Get<long>("unittests.longsetting", 42));
                Assert.Equal(15234, repo.Config.Get<long>("unittests", "longsetting", 42));
            }
        }

        [Fact]
        public void CanReadStringValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Equal("+refs/heads/*:refs/remotes/origin/*", repo.Config.Get<string>("remote.origin.fetch", null));
                Assert.Equal("+refs/heads/*:refs/remotes/origin/*", repo.Config.Get<string>("remote", "origin", "fetch", null));
            }
        }

        [SkippableFact]
        public void CanEnumerateGlobalConfig()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                InconclusiveIf(() => !repo.Config.HasGlobalConfig, "No Git global configuration available");
                var entry = repo.Config.FirstOrDefault(e => e.Key == "user.name");
                Assert.NotNull(entry);
                Assert.NotNull(entry.Value);
            }
        }

        [Fact]
        public void CanEnumerateLocalConfig()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                var entry = repo.Config.FirstOrDefault(e => e.Key == "core.ignorecase");
                Assert.NotNull(entry);
                Assert.Equal("true", entry.Value);
            }
        }

        [Fact]
        public void CanSetBooleanValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.boolsetting", true);

                AssertValueInLocalConfigFile(path.RepositoryPath, "boolsetting = true$");
            }
        }

        [SkippableFact]
        public void CanSetGlobalStringValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                InconclusiveIf(() => !repo.Config.HasGlobalConfig, "No Git global configuration available");

                var existing = repo.Config.Get<string>("user.name", null);
                Assert.NotNull(existing);

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

        [SkippableFact]
        public void CanSetGlobalStringValueWithoutRepo()
        {
            using(var config = new Configuration())
            {
                InconclusiveIf(() => !config.HasGlobalConfig, "No Git global configuration available");

                var existing = config.Get<string>("user.name", null);
                Assert.NotNull(existing);

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

        [Fact]
        public void SettingLocalConfigurationOutsideAReposThrows()
        {
            using (var config = new Configuration())
            {
                Assert.Throws<LibGit2SharpException>(() => config.Set("unittests.intsetting", 3));
            }
        }

        [Fact]
        public void CanSetIntValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.intsetting", 3);

                AssertValueInLocalConfigFile(path.RepositoryPath, "intsetting = 3$");
            }
        }

        [Fact]
        public void CanSetLongValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.longsetting", (long)451);

                AssertValueInLocalConfigFile(path.RepositoryPath, "longsetting = 451");
            }
        }

        [Fact]
        public void CanSetStringValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.stringsetting", "val");

                AssertValueInLocalConfigFile(path.RepositoryPath, "stringsetting = val$");
            }
        }

        [Fact]
        public void CanSetAndReadUnicodeStringValue()
        {
            var path = BuildTemporaryCloneOfTestRepo(StandardTestRepoPath);
            using (var repo = new Repository(path.RepositoryPath))
            {
                repo.Config.Set("unittests.stringsetting", "Juliën");

                AssertValueInLocalConfigFile(path.RepositoryPath, "stringsetting = Juliën$");

                string val = repo.Config.Get("unittests.stringsetting", "");
                Assert.Equal("Juliën", val);
            }

            // Make sure the change is permanent
            using (var repo = new Repository(path.RepositoryPath))
            {
                string val = repo.Config.Get("unittests.stringsetting", "");
                Assert.Equal("Juliën", val);
            }
        }

        [Fact]
        public void ReadingUnsupportedTypeThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Config.Get<short>("unittests.setting", 42));
                Assert.Throws<ArgumentException>(() => repo.Config.Get<Configuration>("unittests.setting", null));
            }
        }

        [Fact]
        public void ReadingValueThatDoesntExistReturnsDefault()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Null(repo.Config.Get<string>("unittests.ghostsetting", null));
                Assert.Equal(0, repo.Config.Get<int>("unittests.ghostsetting", 0));
                Assert.Equal(0L, repo.Config.Get<long>("unittests.ghostsetting", 0L));
                Assert.False(repo.Config.Get<bool>("unittests.ghostsetting", false));
                Assert.Equal("42", repo.Config.Get("unittests.ghostsetting", "42"));
                Assert.Equal(42, repo.Config.Get("unittests.ghostsetting", 42));
                Assert.Equal(42L, repo.Config.Get("unittests.ghostsetting", 42L));
                Assert.True(repo.Config.Get("unittests.ghostsetting", true));
            }
        }

        [Fact]
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
