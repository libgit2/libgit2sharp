using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class ConfigurationFixture : BaseFixture
    {
        private static void AssertValueInLocalConfigFile(string repoPath, string regex)
        {
            var configFilePath = Path.Combine(repoPath, ".git/config");
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.Null(repo.Config.Get<bool>("unittests.boolsetting"));

                repo.Config.Set("unittests.boolsetting", true);
                Assert.True(repo.Config.Get<bool>("unittests.boolsetting").Value);

                repo.Config.Unset("unittests.boolsetting");

                Assert.Null(repo.Config.Get<bool>("unittests.boolsetting"));
            }
        }

        [Fact]
        public void CanUnsetAnEntryFromTheGlobalConfiguration()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            var options = BuildFakeConfigs(scd);

            using (var repo = new Repository(BareTestRepoPath, options))
            {
                Assert.True(repo.Config.HasConfig(ConfigurationLevel.Global));
                Assert.Equal(42, repo.Config.Get<int>("Wow.Man-I-am-totally-global").Value);

                repo.Config.Unset("Wow.Man-I-am-totally-global");
                Assert.Equal(42, repo.Config.Get<int>("Wow.Man-I-am-totally-global").Value);

                repo.Config.Unset("Wow.Man-I-am-totally-global", ConfigurationLevel.Global);
                Assert.Null(repo.Config.Get<int>("Wow.Man-I-am-totally-global"));
            }
        }

        [SkippableFact]
        public void CanGetGlobalStringValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                InconclusiveIf(() => !repo.Config.HasConfig(ConfigurationLevel.Global),
                    "No Git global configuration available");

                Assert.NotNull(repo.Config.Get<string>("user.name"));
            }
        }

        [SkippableFact]
        public void CanGetGlobalStringValueWithoutRepo()
        {
            using (var config = new Configuration())
            {
                InconclusiveIf(() => !config.HasConfig(ConfigurationLevel.Global),
                    "No Git global configuration available");

                Assert.NotNull(config.Get<string>("user.name"));
            }
        }

        [Fact]
        public void CanReadBooleanValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.True(repo.Config.Get<bool>("core.ignorecase").Value);
                Assert.True(repo.Config.GetValueOrDefault<bool>("core.ignorecase"));

                Assert.Equal(false, repo.Config.GetValueOrDefault<bool>("missing.key"));
                Assert.Equal(true, repo.Config.GetValueOrDefault<bool>("missing.key", true));
                Assert.Equal(true, repo.Config.GetValueOrDefault<bool>("missing.key", () => true));
            }
        }

        [Fact]
        public void CanReadIntValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Equal(2, repo.Config.Get<int>("unittests.intsetting").Value);
                Assert.Equal(2, repo.Config.GetValueOrDefault<int>("unittests.intsetting"));
                Assert.Equal(2, repo.Config.GetValueOrDefault<int>("unittests.intsetting", ConfigurationLevel.Local));

                Assert.Equal(0, repo.Config.GetValueOrDefault<int>("missing.key"));
                Assert.Equal(4, repo.Config.GetValueOrDefault<int>("missing.key", 4));
                Assert.Equal(4, repo.Config.GetValueOrDefault<int>("missing.key", () => 4));
            }
        }

        [Fact]
        public void CanReadLongValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Equal(15234, repo.Config.Get<long>("unittests.longsetting").Value);
                Assert.Equal(15234, repo.Config.GetValueOrDefault<long>("unittests.longsetting"));

                Assert.Equal(0, repo.Config.GetValueOrDefault<long>("missing.key"));
                Assert.Equal(4, repo.Config.GetValueOrDefault<long>("missing.key", 4));
                Assert.Equal(4, repo.Config.GetValueOrDefault<long>("missing.key", () => 4));
            }
        }

        [Fact]
        public void CanReadStringValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Equal("+refs/heads/*:refs/remotes/origin/*", repo.Config.Get<string>("remote.origin.fetch").Value);
                Assert.Equal("+refs/heads/*:refs/remotes/origin/*", repo.Config.Get<string>("remote", "origin", "fetch").Value);

                Assert.Equal("+refs/heads/*:refs/remotes/origin/*", repo.Config.GetValueOrDefault<string>("remote.origin.fetch"));
                Assert.Equal("+refs/heads/*:refs/remotes/origin/*", repo.Config.GetValueOrDefault<string>("remote.origin.fetch", ConfigurationLevel.Local));
                Assert.Equal("+refs/heads/*:refs/remotes/origin/*", repo.Config.GetValueOrDefault<string>("remote", "origin", "fetch"));
                Assert.Equal("+refs/heads/*:refs/remotes/origin/*", repo.Config.GetValueOrDefault<string>(new[] { "remote", "origin", "fetch" }));

                Assert.Equal(null, repo.Config.GetValueOrDefault<string>("missing.key"));
                Assert.Equal(null, repo.Config.GetValueOrDefault<string>("missing.key", default(string)));
                Assert.Throws<ArgumentNullException>(() => repo.Config.GetValueOrDefault<string>("missing.key", default(Func<string>)));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>("missing.key", "value"));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>("missing.key", () => "value"));

                Assert.Equal(null, repo.Config.GetValueOrDefault<string>("missing.key", ConfigurationLevel.Local));
                Assert.Equal(null, repo.Config.GetValueOrDefault<string>("missing.key", ConfigurationLevel.Local, default(string)));
                Assert.Throws<ArgumentNullException>(() => repo.Config.GetValueOrDefault<string>("missing.key", ConfigurationLevel.Local, default(Func<string>)));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>("missing.key", ConfigurationLevel.Local, "value"));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>("missing.key", ConfigurationLevel.Local, () => "value"));

                Assert.Equal(null, repo.Config.GetValueOrDefault<string>("missing", "config", "key"));
                Assert.Equal(null, repo.Config.GetValueOrDefault<string>("missing", "config", "key", default(string)));
                Assert.Throws<ArgumentNullException>(() => repo.Config.GetValueOrDefault<string>("missing", "config", "key", default(Func<string>)));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>("missing", "config", "key", "value"));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>("missing", "config", "key", () => "value"));

                Assert.Equal(null, repo.Config.GetValueOrDefault<string>(new[] { "missing", "key" }));
                Assert.Equal(null, repo.Config.GetValueOrDefault<string>(new[] { "missing", "key" }, default(string)));
                Assert.Throws<ArgumentNullException>(() => repo.Config.GetValueOrDefault<string>(new[] { "missing", "key" }, default(Func<string>)));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>(new[] { "missing", "key" }, "value"));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>(new[] { "missing", "key" }, () => "value"));
            }
        }

        [Fact]
        public void CanEnumerateGlobalConfig()
        {
            string configPath = CreateConfigurationWithDummyUser(Constants.Signature);
            var options = new RepositoryOptions { GlobalConfigurationLocation = configPath };

            using (var repo = new Repository(StandardTestRepoPath, options))
            {
                var entry = repo.Config.FirstOrDefault<ConfigurationEntry<string>>(e => e.Key == "user.name");
                Assert.NotNull(entry);
                Assert.Equal(Constants.Signature.Name, entry.Value);
            }
        }

        [Fact]
        public void CanEnumerateLocalConfig()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                var entry = repo.Config.FirstOrDefault<ConfigurationEntry<string>>(e => e.Key == "core.ignorecase");
                Assert.NotNull(entry);
                Assert.Equal("true", entry.Value);
            }
        }

        [Fact]
        public void CanEnumerateLocalConfigContainingAKeyWithNoValue()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var entry = repo.Config
                    .Single<ConfigurationEntry<string>>(c => c.Level == ConfigurationLevel.Local && c.Key == "core.pager");

                Assert.Equal(string.Empty, entry.Value);
            }
        }

        [Fact]
        public void CanFindInLocalConfig()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                var matches = repo.Config.Find("unit");

                Assert.NotNull(matches);
                Assert.Equal(new[] { "unittests.intsetting", "unittests.longsetting" },
                             matches.Select(m => m.Key).ToArray());
            }
        }

        [Fact]
        public void CanFindInGlobalConfig()
        {
            string configPath = CreateConfigurationWithDummyUser(Constants.Signature);
            var options = new RepositoryOptions { GlobalConfigurationLocation = configPath };

            using (var repo = new Repository(StandardTestRepoPath, options))
            {
                var matches = repo.Config.Find(@"\.name", ConfigurationLevel.Global);

                Assert.NotNull(matches);
                Assert.Equal(new[] { "user.name" },
                             matches.Select(m => m.Key).ToArray());
            }
        }

        [Fact]
        public void CanSetBooleanValue()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("unittests.boolsetting", true);

                AssertValueInLocalConfigFile(path, "boolsetting = true$");
            }
        }

        [SkippableFact]
        public void CanSetGlobalStringValue()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                InconclusiveIf(() => !repo.Config.HasConfig(ConfigurationLevel.Global),
                    "No Git global configuration available");

                var existing = repo.Config.Get<string>("user.name");
                Assert.NotNull(existing);

                try
                {
                    repo.Config.Set("user.name", "Unit Test", ConfigurationLevel.Global);

                    AssertValueInGlobalConfigFile("name = Unit Test$");
                }
                finally
                {
                    repo.Config.Set("user.name", existing.Value, ConfigurationLevel.Global);
                }
            }
        }

        [SkippableFact]
        public void CanSetGlobalStringValueWithoutRepo()
        {
            using(var config = new Configuration())
            {
                InconclusiveIf(() => !config.HasConfig(ConfigurationLevel.Global),
                    "No Git global configuration available");

                var existing = config.Get<string>("user.name");
                Assert.NotNull(existing);

                try
                {
                    config.Set("user.name", "Unit Test", ConfigurationLevel.Global);

                    AssertValueInGlobalConfigFile("name = Unit Test$");
                }
                finally
                {
                    config.Set("user.name", existing.Value, ConfigurationLevel.Global);
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
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("unittests.intsetting", 3);

                AssertValueInLocalConfigFile(path, "intsetting = 3$");
            }
        }

        [Fact]
        public void CanSetLongValue()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("unittests.longsetting", (long)451);

                AssertValueInLocalConfigFile(path, "longsetting = 451");
            }
        }

        [Fact]
        public void CanSetStringValue()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("unittests.stringsetting", "val");

                AssertValueInLocalConfigFile(path, "stringsetting = val$");
            }
        }

        [Fact]
        public void CanSetAndReadUnicodeStringValue()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("unittests.stringsetting", "Juliën");

                AssertValueInLocalConfigFile(path, "stringsetting = Juliën$");

                string val = repo.Config.Get<string>("unittests.stringsetting").Value;
                Assert.Equal("Juliën", val);
            }

            // Make sure the change is permanent
            using (var repo = new Repository(path))
            {
                string val = repo.Config.Get<string>("unittests.stringsetting").Value;
                Assert.Equal("Juliën", val);
            }
        }

        [Fact]
        public void ReadingUnsupportedTypeThrows()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Config.Get<short>("unittests.setting"));
                Assert.Throws<ArgumentException>(() => repo.Config.Get<Configuration>("unittests.setting"));
            }
        }

        [Fact]
        public void ReadingValueThatDoesntExistReturnsNull()
        {
            using (var repo = new Repository(StandardTestRepoPath))
            {
                Assert.Null(repo.Config.Get<string>("unittests.ghostsetting"));
                Assert.Null(repo.Config.Get<int>("unittests.ghostsetting"));
                Assert.Null(repo.Config.Get<long>("unittests.ghostsetting"));
                Assert.Null(repo.Config.Get<bool>("unittests.ghostsetting"));
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

        [Fact]
        public void CanGetAnEntryFromASpecificStore()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            var options = BuildFakeConfigs(scd);

            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path, options))
            {
                Assert.True(repo.Config.HasConfig(ConfigurationLevel.Local));
                Assert.True(repo.Config.HasConfig(ConfigurationLevel.Global));
                Assert.True(repo.Config.HasConfig(ConfigurationLevel.System));

                Assert.Null(repo.Config.Get<string>("Woot.global-rocks", ConfigurationLevel.Local));

                repo.Config.Set("Woot.this-rocks", "local");

                Assert.Equal("global", repo.Config.Get<string>("Woot.this-rocks", ConfigurationLevel.Global).Value);
                Assert.Equal("xdg", repo.Config.Get<string>("Woot.this-rocks", ConfigurationLevel.Xdg).Value);
                Assert.Equal("system", repo.Config.Get<string>("Woot.this-rocks", ConfigurationLevel.System).Value);
                Assert.Equal("local", repo.Config.Get<string>("Woot.this-rocks", ConfigurationLevel.Local).Value);
            }
        }

        [Fact]
        public void CanTellIfASpecificStoreContainsAKey()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            var options = BuildFakeConfigs(scd);

            using (var repo = new Repository(BareTestRepoPath, options))
            {
                Assert.True(repo.Config.HasConfig(ConfigurationLevel.System));

                Assert.Null(repo.Config.Get<string>("MCHammer.You-cant-touch-this", ConfigurationLevel.System));
            }
        }
    }
}
