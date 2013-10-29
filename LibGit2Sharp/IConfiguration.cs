using System;
using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides access to configuration variables for a repository.
    /// </summary>
    public interface IConfiguration : IDisposable,
        IEnumerable<ConfigurationEntry<string>>
    {
        /// <summary>
        /// Get a configuration value for a key. Keys are in the form 'section.name'.
        /// <para>
        ///    The same escalation logic than in git.git will be used when looking for the key in the config files:
        ///       - local: the Git file in the current repository
        ///       - global: the Git file specific to the current interactive user (usually in `$HOME/.gitconfig`)
        ///       - xdg: another Git file specific to the current interactive user (usually in `$HOME/.config/git/config`)
        ///       - system: the system-wide Git file
        ///
        ///   The first occurence of the key will be returned.
        /// </para>
        /// <para>
        ///   For example in  order to get the value for this in a .git\config file:
        ///
        ///   <code>
        ///   [core]
        ///   bare = true
        ///   </code>
        ///
        ///   You would call:
        ///
        ///   <code>
        ///   bool isBare = repo.Config.Get&lt;bool&gt;("core.bare").Value;
        ///   </code>
        /// </para>
        /// </summary>
        /// <typeparam name="T">The configuration value type</typeparam>
        /// <param name="key">The key</param>
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        ConfigurationEntry<T> Get<T>(string key);

        /// <summary>
        /// Get a configuration value for a key. Keys are in the form 'section.name'.
        /// <para>
        ///   For example in  order to get the value for this in a .git\config file:
        ///
        ///   <code>
        ///   [core]
        ///   bare = true
        ///   </code>
        ///
        ///   You would call:
        ///
        ///   <code>
        ///   bool isBare = repo.Config.Get&lt;bool&gt;("core.bare").Value;
        ///   </code>
        /// </para>
        /// </summary>
        /// <typeparam name="T">The configuration value type</typeparam>
        /// <param name="key">The key</param>
        /// <param name="level">The configuration file into which the key should be searched for</param>
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        ConfigurationEntry<T> Get<T>(string key, ConfigurationLevel level);

        /// <summary>
        /// Determines which configuration file has been found.
        /// </summary>
        bool HasConfig(ConfigurationLevel level);

        /// <summary>
        /// Set a configuration value for a key. Keys are in the form 'section.name'.
        /// <para>
        ///   For example in order to set the value for this in a .git\config file:
        ///
        ///   [test]
        ///   boolsetting = true
        ///
        ///   You would call:
        ///
        ///   repo.Config.Set("test.boolsetting", true);
        /// </para>
        /// </summary>
        /// <typeparam name="T">The configuration value type</typeparam>
        /// <param name="key">The key parts</param>
        /// <param name="value">The value</param>
        /// <param name="level">The configuration file which should be considered as the target of this operation</param>
        void Set<T>(string key, T value, ConfigurationLevel level = ConfigurationLevel.Local);

        /// <summary>
        /// Unset a configuration variable (key and value).
        /// </summary>
        /// <param name="key">The key to unset.</param>
        /// <param name="level">The configuration file which should be considered as the target of this operation</param>
        void Unset(string key, ConfigurationLevel level = ConfigurationLevel.Local);
    }
}
