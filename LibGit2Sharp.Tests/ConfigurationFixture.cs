using System;
using System.Collections.Generic;
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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
        public void CanAddAndReadMultivarFromTheLocalConfiguration()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.DoesNotContain(repo.Config.OfType<ConfigurationEntry<string>>(), x => x.Key == "unittests.plugin");

                repo.Config.Add("unittests.plugin", "value1", ConfigurationLevel.Local);
                repo.Config.Add("unittests.plugin", "value2", ConfigurationLevel.Local);

                Assert.Equal(new[] { "value1", "value2" }, repo.Config
                    .OfType<ConfigurationEntry<string>>()
                    .Where(x => x.Key == "unittests.plugin" && x.Level == ConfigurationLevel.Local)
                    .Select(x => x.Value)
                    .ToArray());
            }
        }

        [Fact]
        public void CanAddAndReadMultivarFromTheGlobalConfiguration()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.True(repo.Config.HasConfig(ConfigurationLevel.Global));
                Assert.DoesNotContain(repo.Config.OfType<ConfigurationEntry<string>>(), x => x.Key == "unittests.plugin");

                repo.Config.Add("unittests.plugin", "value1", ConfigurationLevel.Global);
                repo.Config.Add("unittests.plugin", "value2", ConfigurationLevel.Global);

                Assert.Equal(new[] { "value1", "value2" }, repo.Config
                    .OfType<ConfigurationEntry<string>>()
                    .Where(x => x.Key == "unittests.plugin")
                    .Select(x => x.Value)
                    .ToArray());
            }
        }

        [Fact]
        public void CanUnsetAllFromTheGlobalConfiguration()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.True(repo.Config.HasConfig(ConfigurationLevel.Global));
                Assert.Empty(repo.Config
                    .OfType<ConfigurationEntry<string>>()
                    .Where(x => x.Key == "unittests.plugin")
                    .Select(x => x.Value)
                    .ToArray());

                repo.Config.Add("unittests.plugin", "value1", ConfigurationLevel.Global);
                repo.Config.Add("unittests.plugin", "value2", ConfigurationLevel.Global);

                Assert.Equal(2, repo.Config
                    .OfType<ConfigurationEntry<string>>()
                    .Where(x => x.Key == "unittests.plugin" && x.Level == ConfigurationLevel.Global)
                    .Select(x => x.Value)
                    .Count());

                repo.Config.UnsetAll("unittests.plugin");

                Assert.Equal(2, repo.Config
                    .OfType<ConfigurationEntry<string>>()
                    .Where(x => x.Key == "unittests.plugin" && x.Level == ConfigurationLevel.Global)
                    .Select(x => x.Value)
                    .Count());

                repo.Config.UnsetAll("unittests.plugin", ConfigurationLevel.Global);

                Assert.Empty(repo.Config
                    .OfType<ConfigurationEntry<string>>()
                    .Where(x => x.Key == "unittests.plugin")
                    .Select(x => x.Value)
                    .ToArray());
            }
        }

        [Fact]
        public void CanUnsetAllFromTheLocalConfiguration()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Assert.True(repo.Config.HasConfig(ConfigurationLevel.Global));
                Assert.Empty(repo.Config
                    .OfType<ConfigurationEntry<string>>()
                    .Where(x => x.Key == "unittests.plugin")
                    .Select(x => x.Value)
                    .ToArray());

                repo.Config.Add("unittests.plugin", "value1");
                repo.Config.Add("unittests.plugin", "value2");

                Assert.Equal(2, repo.Config
                    .OfType<ConfigurationEntry<string>>()
                    .Where(x => x.Key == "unittests.plugin" && x.Level == ConfigurationLevel.Local)
                    .Select(x => x.Value)
                    .Count());

                repo.Config.UnsetAll("unittests.plugin");

                Assert.DoesNotContain(repo.Config.OfType<ConfigurationEntry<string>>(), x => x.Key == "unittests.plugin");
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

                Assert.False(repo.Config.GetValueOrDefault<bool>("missing.key"));
                Assert.True(repo.Config.GetValueOrDefault<bool>("missing.key", true));
                Assert.True(repo.Config.GetValueOrDefault<bool>("missing.key", () => true));
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

                Assert.Null(repo.Config.GetValueOrDefault<string>("missing.key"));
                Assert.Null(repo.Config.GetValueOrDefault<string>("missing.key", default(string)));
                Assert.Throws<ArgumentNullException>(() => repo.Config.GetValueOrDefault<string>("missing.key", default(Func<string>)));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>("missing.key", "value"));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>("missing.key", () => "value"));

                Assert.Null(repo.Config.GetValueOrDefault<string>("missing.key", ConfigurationLevel.Local));
                Assert.Null(repo.Config.GetValueOrDefault<string>("missing.key", ConfigurationLevel.Local, default(string)));
                Assert.Throws<ArgumentNullException>(() => repo.Config.GetValueOrDefault<string>("missing.key", ConfigurationLevel.Local, default(Func<string>)));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>("missing.key", ConfigurationLevel.Local, "value"));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>("missing.key", ConfigurationLevel.Local, () => "value"));

                Assert.Null(repo.Config.GetValueOrDefault<string>("missing", "config", "key"));
                Assert.Null(repo.Config.GetValueOrDefault<string>("missing", "config", "key", default(string)));
                Assert.Throws<ArgumentNullException>(() => repo.Config.GetValueOrDefault<string>("missing", "config", "key", default(Func<string>)));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>("missing", "config", "key", "value"));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>("missing", "config", "key", () => "value"));

                Assert.Null(repo.Config.GetValueOrDefault<string>(new[] { "missing", "key" }));
                Assert.Null(repo.Config.GetValueOrDefault<string>(new[] { "missing", "key" }, default(string)));
                Assert.Throws<ArgumentNullException>(() => repo.Config.GetValueOrDefault<string>(new[] { "missing", "key" }, default(Func<string>)));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>(new[] { "missing", "key" }, "value"));
                Assert.Equal("value", repo.Config.GetValueOrDefault<string>(new[] { "missing", "key" }, () => "value"));
            }
        }

        [Fact]
        public void CanEnumerateGlobalConfig()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                CreateConfigurationWithDummyUser(repo, Constants.Identity);
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

            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                var matches = repo.Config.Find("-rocks", ConfigurationLevel.Global);

                Assert.NotNull(matches);
                Assert.Equal(new[] { "woot.this-rocks" },
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
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
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

        [Theory, MemberData(nameof(ConfigAccessors))]
        public void CanAccessConfigurationWithoutARepository(Func<string, string> localConfigurationPathProvider)
        {
            var path = SandboxStandardTestRepoGitDir();

            using (var repo = new Repository(path))
            {
                repo.Config.Set("my.key", "local");
                repo.Config.Set("my.key", "mouse", ConfigurationLevel.Global);
            }

            var globalPath = Path.Combine(GlobalSettings.GetConfigSearchPaths(ConfigurationLevel.Global).Single(), ".gitconfig");
            using (var config = Configuration.BuildFrom(localConfigurationPathProvider(path), globalPath))
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

            using (var repo = new Repository(repoPath))
            {
                CreateConfigurationWithDummyUser(repo, name, email);
                Assert.Equal(name, repo.Config.GetValueOrDefault<string>("user.name"));
                Assert.Equal(email, repo.Config.GetValueOrDefault<string>("user.email"));

                Signature signature = repo.Config.BuildSignature(DateTimeOffset.Now);

                Assert.Null(signature);
            }
        }

        [Fact]
        public void CanSetAndGetSearchPath()
        {
            string globalPath = Path.Combine(Constants.TemporaryReposPath, Path.GetRandomFileName());
            string systemPath = Path.Combine(Constants.TemporaryReposPath, Path.GetRandomFileName());
            string xdgPath = Path.Combine(Constants.TemporaryReposPath, Path.GetRandomFileName());

            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, globalPath);
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.System, systemPath);
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Xdg, xdgPath);

            Assert.Equal(globalPath, GlobalSettings.GetConfigSearchPaths(ConfigurationLevel.Global).Single());
            Assert.Equal(systemPath, GlobalSettings.GetConfigSearchPaths(ConfigurationLevel.System).Single());
            Assert.Equal(xdgPath, GlobalSettings.GetConfigSearchPaths(ConfigurationLevel.Xdg).Single());

            // reset the search paths to their defaults
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, null);
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.System, null);
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Xdg, null);
        }

        [Fact]
        public void CanSetAndGetMultipleSearchPaths()
        {
            string[] paths =
            {
                Path.Combine(Constants.TemporaryReposPath, Path.GetRandomFileName()),
                Path.Combine(Constants.TemporaryReposPath, Path.GetRandomFileName()),
                Path.Combine(Constants.TemporaryReposPath, Path.GetRandomFileName()),
            };

            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, paths);

            Assert.Equal(paths, GlobalSettings.GetConfigSearchPaths(ConfigurationLevel.Global));

            // set back to the defaults
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, null);
        }

        [Fact]
        public void CanResetSearchPaths()
        {
            // record the default search path
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, null);
            var oldPaths = GlobalSettings.GetConfigSearchPaths(ConfigurationLevel.Global);
            Assert.NotNull(oldPaths);

            // generate a non-default path to set
            var newPaths = new string[] { Path.Combine(Constants.TemporaryReposPath, Path.GetRandomFileName()) };

            // change to the non-default path
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, newPaths);
            Assert.Equal(newPaths, GlobalSettings.GetConfigSearchPaths(ConfigurationLevel.Global));

            // set it back to the default
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, null);
            Assert.Equal(oldPaths, GlobalSettings.GetConfigSearchPaths(ConfigurationLevel.Global));
        }

        [Fact]
        public void CanAppendToSearchPaths()
        {
            string appendMe = Path.Combine(Constants.TemporaryReposPath, Path.GetRandomFileName());
            var prevPaths = GlobalSettings.GetConfigSearchPaths(ConfigurationLevel.Global);

            // append using the special name $PATH
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, "$PATH", appendMe);

            var currentPaths = GlobalSettings.GetConfigSearchPaths(ConfigurationLevel.Global);
            Assert.Equal(prevPaths.Concat(new[] { appendMe }), currentPaths);

            // set it back to the default
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, null);
        }

        [Fact]
        public void CanRedirectConfigAccess()
        {
            var scd1 = BuildSelfCleaningDirectory();
            var scd2 = BuildSelfCleaningDirectory();

            Touch(scd1.RootedDirectoryPath, ".gitconfig");
            Touch(scd2.RootedDirectoryPath, ".gitconfig");

            // redirect global access to the first path
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, scd1.RootedDirectoryPath);

            // set a value in the first config
            using (var config = Configuration.BuildFrom(null))
            {
                config.Set("luggage.code", 9876, ConfigurationLevel.Global);
                Assert.Equal(9876, config.Get<int>("luggage.code", ConfigurationLevel.Global).Value);
            }

            // redirect global config access to path2
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, scd2.RootedDirectoryPath);

            // if the redirect succeeds, the value set in the prior config should not be visible
            using (var config = Configuration.BuildFrom(null))
            {
                Assert.Equal(-1, config.GetValueOrDefault<int>("luggage.code", ConfigurationLevel.Global, -1));
            }

            // reset the search path to the default
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, null);
        }
    }
}
