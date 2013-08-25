using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides helper overloads to a <see cref="Configuration"/>.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Get a configuration value for the given key parts.
        /// <para>
        ///   For example in order to get the value for this in a .git\config file:
        ///
        ///   <code>
        ///   [core]
        ///   bare = true
        ///   </code>
        ///
        ///   You would call:
        ///
        ///   <code>
        ///   bool isBare = repo.Config.Get&lt;bool&gt;(new []{ "core", "bare" }).Value;
        ///   </code>
        /// </para>
        /// </summary>
        /// <typeparam name="T">The configuration value type</typeparam>
        /// <param name="config">The configuration being worked with.</param>
        /// <param name="keyParts">The key parts</param>
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        public static ConfigurationEntry<T> Get<T>(this Configuration config, string[] keyParts)
        {
            Ensure.ArgumentNotNull(keyParts, "keyParts");

            return config.Get<T>(string.Join(".", keyParts));
        }

        /// <summary>
        /// Get a configuration value for the given key parts.
        /// <para>
        ///   For example in order to get the value for this in a .git\config file:
        ///
        ///   <code>
        ///   [difftool "kdiff3"]
        ///     path = c:/Program Files/KDiff3/kdiff3.exe
        ///   </code>
        ///
        ///   You would call:
        ///
        ///   <code>
        ///   string where = repo.Config.Get&lt;string&gt;("difftool", "kdiff3", "path").Value;
        ///   </code>
        /// </para>
        /// </summary>
        /// <typeparam name="T">The configuration value type</typeparam>
        /// <param name="config">The configuration being worked with.</param>
        /// <param name="firstKeyPart">The first key part</param>
        /// <param name="secondKeyPart">The second key part</param>
        /// <param name="thirdKeyPart">The third key part</param>
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        public static ConfigurationEntry<T> Get<T>(this Configuration config, string firstKeyPart, string secondKeyPart, string thirdKeyPart)
        {
            Ensure.ArgumentNotNullOrEmptyString(firstKeyPart, "firstKeyPart");
            Ensure.ArgumentNotNullOrEmptyString(secondKeyPart, "secondKeyPart");
            Ensure.ArgumentNotNullOrEmptyString(thirdKeyPart, "secondKeyPart");

            return config.Get<T>(new[] { firstKeyPart, secondKeyPart, thirdKeyPart });
        }

        /// <summary>
        /// Get a configuration value for the given key,
        /// or <paramref name="defaultValue" /> if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="config">The configuration being worked with.</param>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key is not set.</param>
        /// <returns>The configuration value, or the default.</returns>
        public static T GetValueOrDefault<T>(this Configuration config, string key, T defaultValue = default(T))
        {
            return ValueOrDefault(config.Get<T>(key), defaultValue);
        }

        /// <summary>
        /// Get a configuration value for the given key,
        /// or <paramref name="defaultValue" /> if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        ///  <param name="config">The configuration being worked with.</param>
        /// <param name="key">The key.</param>
        /// <param name="level">The configuration file into which the key should be searched for.</param>
        /// <param name="defaultValue">The selector used to generate a default value if the key is not set.</param>
        /// <returns>The configuration value, or the default.</returns>
        public static T GetValueOrDefault<T>(this Configuration config, string key, ConfigurationLevel level, T defaultValue = default(T))
        {
            return ValueOrDefault(config.Get<T>(key, level), defaultValue);
        }

        /// <summary>
        /// Get a configuration value for the given key parts,
        /// or <paramref name="defaultValue" /> if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="config">The configuration being worked with.</param>
        /// <param name="keyParts">The key parts.</param>
        /// <param name="defaultValue">The default value if the key is not set.</param>
        /// <returns>The configuration value, or the default.</returns>
        public static T GetValueOrDefault<T>(this Configuration config, string[] keyParts, T defaultValue = default(T))
        {
            return ValueOrDefault(config.Get<T>(keyParts), defaultValue);
        }

        /// <summary>
        /// Get a configuration value for the given key parts,
        /// or <paramref name="defaultValue" /> if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="config">The configuration being worked with.</param>
        /// <param name="firstKeyPart">The first key part.</param>
        /// <param name="secondKeyPart">The second key part.</param>
        /// <param name="thirdKeyPart">The third key part.</param>
        /// <param name="defaultValue">The default value if the key is not set.</param>
        /// <returns>The configuration value, or the default.</returns>
        public static T GetValueOrDefault<T>(this Configuration config, string firstKeyPart, string secondKeyPart, string thirdKeyPart, T defaultValue = default(T))
        {
            return ValueOrDefault(config.Get<T>(firstKeyPart, secondKeyPart, thirdKeyPart), defaultValue);
        }

        /// <summary>
        /// Get a configuration value for the given key,
        /// or a value generated by <paramref name="defaultValueSelector" />
        /// if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="config">The configuration being worked with.</param>
        /// <param name="key">The key</param>
        /// <param name="defaultValueSelector">The selector used to generate a default value if the key is not set.</param>
        /// <returns>The configuration value, or a generated default.</returns>
        public static T GetValueOrDefault<T>(this Configuration config, string key, Func<T> defaultValueSelector)
        {
            return ValueOrDefault(config.Get<T>(key), defaultValueSelector);
        }

        /// <summary>
        /// Get a configuration value for the given key,
        /// or a value generated by <paramref name="defaultValueSelector" />
        /// if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        ///  <param name="config">The configuration being worked with.</param>
        /// <param name="key">The key.</param>
        /// <param name="level">The configuration file into which the key should be searched for.</param>
        /// <param name="defaultValueSelector">The selector used to generate a default value if the key is not set.</param>
        /// <returns>The configuration value, or a generated default.</returns>
        public static T GetValueOrDefault<T>(this Configuration config, string key, ConfigurationLevel level, Func<T> defaultValueSelector)
        {
            return ValueOrDefault(config.Get<T>(key, level), defaultValueSelector);
        }

        /// <summary>
        /// Get a configuration value for the given key parts,
        /// or a value generated by <paramref name="defaultValueSelector" />
        /// if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="config">The configuration being worked with.</param>
        /// <param name="keyParts">The key parts.</param>
        /// <param name="defaultValueSelector">The selector used to generate a default value if the key is not set.</param>
        /// <returns>The configuration value, or a generated default.</returns>
        public static T GetValueOrDefault<T>(this Configuration config, string[] keyParts, Func<T> defaultValueSelector)
        {
            return ValueOrDefault(config.Get<T>(keyParts), defaultValueSelector);
        }

        /// <summary>
        /// Get a configuration value for the given key parts,
        /// or a value generated by <paramref name="defaultValueSelector" />
        /// if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="config">The configuration being worked with.</param>
        /// <param name="firstKeyPart">The first key part.</param>
        /// <param name="secondKeyPart">The second key part.</param>
        /// <param name="thirdKeyPart">The third key part.</param>
        /// <param name="defaultValueSelector">The selector used to generate a default value if the key is not set.</param>
        /// <returns>The configuration value, or a generated default.</returns>
        public static T GetValueOrDefault<T>(this Configuration config, string firstKeyPart, string secondKeyPart, string thirdKeyPart, Func<T> defaultValueSelector)
        {
            return ValueOrDefault(config.Get<T>(firstKeyPart, secondKeyPart, thirdKeyPart), defaultValueSelector);
        }

        private static T ValueOrDefault<T>(ConfigurationEntry<T> value, T defaultValue)
        {
            return value == null ? defaultValue : value.Value;
        }

        private static T ValueOrDefault<T>(ConfigurationEntry<T> value, Func<T> defaultValueSelector)
        {
            Ensure.ArgumentNotNull(defaultValueSelector, "defaultValueSelector");

            return value == null
                       ? defaultValueSelector()
                       : value.Value;
        }
    }
}
