using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// The full representation of a config option.
    /// </summary>
    /// <typeparam name="T">The configuration value type</typeparam>
    public class ConfigurationEntry<T>
    {
        /// <summary>
        /// The fully-qualified option name.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// The option value.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// The origin store.
        /// </summary>
        public ConfigurationLevel Level { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationEntry{T}"/> class with a given key and value
        /// </summary>
        /// <param name="key">The option name</param>
        /// <param name="value">The option value</param>
        /// <param name="level">The origin store</param>
        public ConfigurationEntry(string key, T value, ConfigurationLevel level)
        {
            Key = key;
            Value = value;
            Level = level;
        }
    }

    /// <summary>
    /// Enumerated config option
    /// </summary>
    [Obsolete("This class will be removed in the next release. Please use ConfigurationEntry<T> instead.")]
    public class ConfigurationEntry : ConfigurationEntry<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationEntry{T}"/> class with a given key and value
        /// </summary>
        /// <param name="key">The option name</param>
        /// <param name="value">The option value</param>
        /// <param name="level">The origin store</param>
        public ConfigurationEntry(string key, string value, ConfigurationLevel level) : base(key, value, level)
        { }
    }
}
