using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class ConfigurationEntryFixture : BaseFixture
    {
        private const string testKey = "testKey";
        private const string testStringValue = "testValue";
        private const int testIntValue = 1209812;
        private const ConfigurationLevel testLevel = ConfigurationLevel.Local;

        private static ConfigurationEntry<T> GetTestConfigurationEntry<T>()
        {
            if (typeof(T) == typeof(string))
            {
                return new ConfigurationEntry<string>(testKey, testStringValue, testLevel) as ConfigurationEntry<T>;
            }
            else if (typeof(T) == typeof(int))
            {
                return new ConfigurationEntry<int>(testKey, testIntValue, testLevel) as ConfigurationEntry<T>;
            }
            else
            {
                return null;
            }
        }

        [Fact]
        public void SetsProperties()
        {
            ConfigurationEntry<string> configurationEntry = ConfigurationEntryFixture.GetTestConfigurationEntry<string>();
            Assert.Equal<string>(testKey, configurationEntry.Key);
            Assert.Equal<string>(testStringValue, configurationEntry.Value);
            Assert.Equal<ConfigurationLevel>(testLevel, configurationEntry.Level);
        }

        [Fact]
        public void SetsPropertiesForPrimitiveType()
        {
            ConfigurationEntry<int> configurationEntry = ConfigurationEntryFixture.GetTestConfigurationEntry<int>();
            Assert.Equal<string>(testKey, configurationEntry.Key);
            Assert.Equal<int>(testIntValue, configurationEntry.Value);
            Assert.Equal<ConfigurationLevel>(testLevel, configurationEntry.Level);
        }

        [Fact]
        public void GetsValueOrDefaultForNonNull()
        {
            ConfigurationEntry<int> configurationEntry = ConfigurationEntryFixture.GetTestConfigurationEntry<int>();
            int value = ConfigurationEntry<int>.ValueOrDefault(configurationEntry);
            Assert.Equal<int>(testIntValue, value);
        }

        [Fact]
        public void GetsValueOrDefaultForNull()
        {
            string value = ConfigurationEntry<string>.ValueOrDefault(null);
            Assert.Equal<string>(null, value);
        }

        [Fact]
        public void GetsValueOrDefaultForPrimitiveNull()
        {
            int value = ConfigurationEntry<int>.ValueOrDefault(null);
            Assert.Equal<int>(0, value);
        }
    }
}
