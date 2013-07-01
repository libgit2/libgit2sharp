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
    }
}
