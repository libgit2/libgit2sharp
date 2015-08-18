using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class ConfigurationFixture : BaseFixture
    {
        private static void AssertValueInLocalConfigFile(string repoPath, string regex)
        {
            var configFilePath = Path.Combine(repoPath, ".git/config");
            AssertValueInConfigFile(configFilePath, regex);
        }

        [Fact]
        public void CanUnsetAnEntryFromTheLocalConfiguration()
        {
            string path = SandboxStandardTestRepo();
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

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, options))
            {
                Assert.True(repo.Config.HasConfig(ConfigurationLevel.Global));
                Assert.Equal(42, repo.Config.Get<int>("Wow.Man-I-am-totally-global").Value);

                repo.Config.Unset("Wow.Man-I-am-totally-global");
                Assert.Equal(42, repo.Config.Get<int>("Wow.Man-I-am-totally-global").Value);

                repo.Config.Unset("Wow.Man-I-am-totally-global", ConfigurationLevel.Global);
                Assert.Null(repo.Config.Get<int>("Wow.Man-I-am-totally-global"));
            }
        }

        [Fact]
        public void CanReadBooleanValue()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
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
            string configPath = CreateConfigurationWithDummyUser(Constants.Identity);
            var options = new RepositoryOptions { GlobalConfigurationLocation = configPath };

            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path, options))
            {
                var entry = repo.Config.FirstOrDefault<ConfigurationEntry<string>>(e => e.Key == "user.name");
                Assert.NotNull(entry);
                Assert.Equal(Constants.Signature.Name, entry.Value);
            }
        }

        [Fact]
        public void CanEnumerateLocalConfig()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                var entry = repo.Config.FirstOrDefault<ConfigurationEntry<string>>(e => e.Key == "core.ignorecase");
                Assert.NotNull(entry);
                Assert.Equal("true", entry.Value);
            }
        }

        [Fact]
        public void CanEnumerateLocalConfigContainingAKeyWithNoValue()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var entry = repo.Config
                    .Single<ConfigurationEntry<string>>(c => c.Level == ConfigurationLevel.Local && c.Key == "core.pager");

                Assert.Equal(string.Empty, entry.Value);
            }
        }

        [Fact]
        public void CanFindInLocalConfig()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                var matches = repo.Config.Find("unit");

                Assert.NotNull(matches);
                Assert.Equal(new[] { "unittests.intsetting", "unittests.longsetting" },
                             matches
                             .Select(m => m.Key)
                             .OrderBy(s => s)
                             .ToArray());
            }
        }

        [Fact]
        public void CanFindInGlobalConfig()
        {
            string configPath = CreateConfigurationWithDummyUser(Constants.Identity);
            var options = new RepositoryOptions { GlobalConfigurationLocation = configPath };

            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path, options))
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
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("unittests.boolsetting", true);

                AssertValueInLocalConfigFile(path, "boolsetting = true$");
            }
        }

        [Fact]
        public void SettingLocalConfigurationOutsideAReposThrows()
        {
            using (var config = Configuration.BuildFrom(null, null, null, null))
            {
                Assert.Throws<LibGit2SharpException>(() => config.Set("unittests.intsetting", 3));
            }
        }

        [Fact]
        public void CanSetIntValue()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("unittests.intsetting", 3);

                AssertValueInLocalConfigFile(path, "intsetting = 3$");
            }
        }

        [Fact]
        public void CanSetLongValue()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("unittests.longsetting", (long)451);

                AssertValueInLocalConfigFile(path, "longsetting = 451");
            }
        }

        [Fact]
        public void CanSetStringValue()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("unittests.stringsetting", "val");

                AssertValueInLocalConfigFile(path, "stringsetting = val$");
            }
        }

        [Fact]
        public void CanSetAndReadUnicodeStringValue()
        {
            string path = SandboxStandardTestRepo();
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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                Assert.Throws<ArgumentException>(() => repo.Config.Get<short>("unittests.setting"));
                Assert.Throws<ArgumentException>(() => repo.Config.Get<Configuration>("unittests.setting"));
            }
        }

        [Fact]
        public void ReadingValueThatDoesntExistReturnsNull()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
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
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
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

            string path = SandboxStandardTestRepo();
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

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, options))
            {
                Assert.True(repo.Config.HasConfig(ConfigurationLevel.System));

                Assert.Null(repo.Config.Get<string>("MCHammer.You-cant-touch-this", ConfigurationLevel.System));
            }
        }

        public static IEnumerable<object[]> ConfigAccessors
        {
            get
            {
                return new List<object[]>
                {
                    new[] { new Func<string, string>(p => Path.Combine(p, ".git", "config")) },
                    new[] { new Func<string, string>(p => Path.Combine(p, ".git")) },
                    new[] { new Func<string, string>(p => p) },
                };
            }
        }

        [Theory, PropertyData("ConfigAccessors")]
        public void CanAccessConfigurationWithoutARepository(Func<string, string> localConfigurationPathProvider)
        {
            var path = SandboxStandardTestRepoGitDir();

            string globalConfigPath = CreateConfigurationWithDummyUser(Constants.Identity);
            var options = new RepositoryOptions { GlobalConfigurationLocation = globalConfigPath };

            using (var repo = new Repository(path, options))
            {
                repo.Config.Set("my.key", "local");
                repo.Config.Set("my.key", "mouse", ConfigurationLevel.Global);
            }

            using (var config = Configuration.BuildFrom(localConfigurationPathProvider(path), globalConfigPath))
            {
                Assert.Equal("local", config.Get<string>("my.key").Value);
                Assert.Equal("mouse", config.Get<string>("my.key", ConfigurationLevel.Global).Value);
            }
        }

        [Fact]
        public void PassingANonExistingLocalConfigurationFileToBuildFromthrowss()
        {
            Assert.Throws<FileNotFoundException>(() => Configuration.BuildFrom(
                Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())));
        }

        [Theory]
        [InlineData(null, "x@example.com")]
        [InlineData("", "x@example.com")]
        [InlineData("X", null)]
        [InlineData("X", "")]
        public void CannotBuildAProperSignatureFromConfigWhenFullIdentityCannotBeFoundInTheConfig(string name, string email)
        {
            string repoPath = InitNewRepository();
            string configPath = CreateConfigurationWithDummyUser(name, email);
            var options = new RepositoryOptions { GlobalConfigurationLocation = configPath };

            using (var repo = new Repository(repoPath, options))
            {
                Assert.Equal(name, repo.Config.GetValueOrDefault<string>("user.name"));
                Assert.Equal(email, repo.Config.GetValueOrDefault<string>("user.email"));

                Signature signature = repo.Config.BuildSignature(DateTimeOffset.Now);

                Assert.Null(signature);
            }
        }
    }
}
