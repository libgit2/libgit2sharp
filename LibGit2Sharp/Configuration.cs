using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides access to configuration variables for a repository.
    /// </summary>
    public class Configuration : IDisposable,
        IEnumerable<ConfigurationEntry<string>>
    {
        private readonly FilePath repoConfigPath;
        private readonly FilePath globalConfigPath;
        private readonly FilePath xdgConfigPath;
        private readonly FilePath systemConfigPath;

        private ConfigurationSafeHandle configHandle;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Configuration()
        { }

        internal Configuration(
            Repository repository,
            string repositoryConfigurationFileLocation,
            string globalConfigurationFileLocation,
            string xdgConfigurationFileLocation,
            string systemConfigurationFileLocation)
        {
            if (repositoryConfigurationFileLocation != null)
            {
                repoConfigPath = NormalizeConfigPath(repositoryConfigurationFileLocation);
            }

            globalConfigPath = globalConfigurationFileLocation ?? Proxy.git_config_find_global();
            xdgConfigPath = xdgConfigurationFileLocation ?? Proxy.git_config_find_xdg();
            systemConfigPath = systemConfigurationFileLocation ?? Proxy.git_config_find_system();

            Init(repository);
        }

        private void Init(Repository repository)
        {
            configHandle = Proxy.git_config_new();

            if (repository != null)
            {
                //TODO: push back this logic into libgit2.
                // As stated by @carlosmn "having a helper function to load the defaults and then allowing you
                // to modify it before giving it to git_repository_open_ext() would be a good addition, I think."
                //  -- Agreed :)
                string repoConfigLocation = Path.Combine(repository.Info.Path, "config");
                Proxy.git_config_add_file_ondisk(configHandle, repoConfigLocation, ConfigurationLevel.Local);

                Proxy.git_repository_set_config(repository.Handle, configHandle);
            }
            else if (repoConfigPath != null)
            {
                Proxy.git_config_add_file_ondisk(configHandle, repoConfigPath, ConfigurationLevel.Local);
            }

            if (globalConfigPath != null)
            {
                Proxy.git_config_add_file_ondisk(configHandle, globalConfigPath, ConfigurationLevel.Global);
            }

            if (xdgConfigPath != null)
            {
                Proxy.git_config_add_file_ondisk(configHandle, xdgConfigPath, ConfigurationLevel.Xdg);
            }

            if (systemConfigPath != null)
            {
                Proxy.git_config_add_file_ondisk(configHandle, systemConfigPath, ConfigurationLevel.System);
            }
        }

        private FilePath NormalizeConfigPath(FilePath path)
        {
            if (File.Exists(path.Native))
            {
                return path;
            }

            if (!Directory.Exists(path.Native))
            {
                throw new FileNotFoundException("Cannot find repository configuration file", path.Native);
            }

            var configPath = Path.Combine(path.Native, "config");

            if (File.Exists(configPath))
            {
                return configPath;
            }

            var gitConfigPath = Path.Combine(path.Native, ".git", "config");

            if (File.Exists(gitConfigPath))
            {
                return gitConfigPath;
            }

            throw new FileNotFoundException("Cannot find repository configuration file", path.Native);
        }

        /// <summary>
        /// Access configuration values without a repository.
        /// <para>
        ///   Generally you want to access configuration via an instance of <see cref="Repository"/> instead.
        /// </para>
        /// <para>
        ///   <paramref name="repositoryConfigurationFileLocation"/> can either contains a path to a file or a directory. In the latter case,
        ///   this can be the working directory, the .git directory or the directory containing a bare repository.
        /// </para>
        /// </summary>
        /// <param name="repositoryConfigurationFileLocation">Path to an existing Repository configuration file.</param>
        /// <returns>An instance of <see cref="Configuration"/>.</returns>
        public static Configuration BuildFrom(string repositoryConfigurationFileLocation)
        {
            return BuildFrom(repositoryConfigurationFileLocation, null, null, null);
        }

        /// <summary>
        /// Access configuration values without a repository.
        /// <para>
        ///   Generally you want to access configuration via an instance of <see cref="Repository"/> instead.
        /// </para>
        /// <para>
        ///   <paramref name="repositoryConfigurationFileLocation"/> can either contains a path to a file or a directory. In the latter case,
        ///   this can be the working directory, the .git directory or the directory containing a bare repository.
        /// </para>
        /// </summary>
        /// <param name="repositoryConfigurationFileLocation">Path to an existing Repository configuration file.</param>
        /// <param name="globalConfigurationFileLocation">Path to a Global configuration file. If null, the default path for a Global configuration file will be probed.</param>
        /// <returns>An instance of <see cref="Configuration"/>.</returns>
        public static Configuration BuildFrom(
            string repositoryConfigurationFileLocation,
            string globalConfigurationFileLocation)
        {
            return BuildFrom(repositoryConfigurationFileLocation, globalConfigurationFileLocation, null, null);
        }

        /// <summary>
        /// Access configuration values without a repository.
        /// <para>
        ///   Generally you want to access configuration via an instance of <see cref="Repository"/> instead.
        /// </para>
        /// <para>
        ///   <paramref name="repositoryConfigurationFileLocation"/> can either contains a path to a file or a directory. In the latter case,
        ///   this can be the working directory, the .git directory or the directory containing a bare repository.
        /// </para>
        /// </summary>
        /// <param name="repositoryConfigurationFileLocation">Path to an existing Repository configuration file.</param>
        /// <param name="globalConfigurationFileLocation">Path to a Global configuration file. If null, the default path for a Global configuration file will be probed.</param>
        /// <param name="xdgConfigurationFileLocation">Path to a XDG configuration file. If null, the default path for a XDG configuration file will be probed.</param>
        /// <returns>An instance of <see cref="Configuration"/>.</returns>
        public static Configuration BuildFrom(
            string repositoryConfigurationFileLocation,
            string globalConfigurationFileLocation,
            string xdgConfigurationFileLocation)
        {
            return BuildFrom(repositoryConfigurationFileLocation, globalConfigurationFileLocation, xdgConfigurationFileLocation, null);
        }

        /// <summary>
        /// Access configuration values without a repository.
        /// <para>
        ///   Generally you want to access configuration via an instance of <see cref="Repository"/> instead.
        /// </para>
        /// <para>
        ///   <paramref name="repositoryConfigurationFileLocation"/> can either contains a path to a file or a directory. In the latter case,
        ///   this can be the working directory, the .git directory or the directory containing a bare repository.
        /// </para>
        /// </summary>
        /// <param name="repositoryConfigurationFileLocation">Path to an existing Repository configuration file.</param>
        /// <param name="globalConfigurationFileLocation">Path to a Global configuration file. If null, the default path for a Global configuration file will be probed.</param>
        /// <param name="xdgConfigurationFileLocation">Path to a XDG configuration file. If null, the default path for a XDG configuration file will be probed.</param>
        /// <param name="systemConfigurationFileLocation">Path to a System configuration file. If null, the default path for a System configuration file will be probed.</param>
        /// <returns>An instance of <see cref="Configuration"/>.</returns>
        public static Configuration BuildFrom(
            string repositoryConfigurationFileLocation,
            string globalConfigurationFileLocation,
            string xdgConfigurationFileLocation,
            string systemConfigurationFileLocation)
        {
            return new Configuration(null, repositoryConfigurationFileLocation, globalConfigurationFileLocation, xdgConfigurationFileLocation, systemConfigurationFileLocation);
        }

        /// <summary>
        /// Access configuration values without a repository. Generally you want to access configuration via an instance of <see cref="Repository"/> instead.
        /// </summary>
        /// <param name="globalConfigurationFileLocation">Path to a Global configuration file. If null, the default path for a global configuration file will be probed.</param>
        [Obsolete("This method will be removed in the next release. Please use Configuration.BuildFrom(string, string) instead.")]
        public Configuration(string globalConfigurationFileLocation)
            : this(null, null, globalConfigurationFileLocation, null, null)
        { }

        /// <summary>
        /// Access configuration values without a repository. Generally you want to access configuration via an instance of <see cref="Repository"/> instead.
        /// </summary>
        /// <param name="globalConfigurationFileLocation">Path to a Global configuration file. If null, the default path for a global configuration file will be probed.</param>
        /// <param name="xdgConfigurationFileLocation">Path to a XDG configuration file. If null, the default path for a XDG configuration file will be probed.</param>
        [Obsolete("This method will be removed in the next release. Please use Configuration.BuildFrom(string, string, string) instead.")]
        public Configuration(string globalConfigurationFileLocation, string xdgConfigurationFileLocation)
            : this(null, null, globalConfigurationFileLocation, xdgConfigurationFileLocation, null)
        { }

        /// <summary>
        /// Access configuration values without a repository. Generally you want to access configuration via an instance of <see cref="Repository"/> instead.
        /// </summary>
        /// <param name="globalConfigurationFileLocation">Path to a Global configuration file. If null, the default path for a global configuration file will be probed.</param>
        /// <param name="xdgConfigurationFileLocation">Path to a XDG configuration file. If null, the default path for a XDG configuration file will be probed.</param>
        /// <param name="systemConfigurationFileLocation">Path to a System configuration file. If null, the default path for a system configuration file will be probed.</param>
        [Obsolete("This method will be removed in the next release. Please use Configuration.BuildFrom(string, string, string, string) instead.")]
        public Configuration(string globalConfigurationFileLocation, string xdgConfigurationFileLocation, string systemConfigurationFileLocation)
            : this(null, null, globalConfigurationFileLocation, xdgConfigurationFileLocation, systemConfigurationFileLocation)
        { }

        /// <summary>
        /// Determines which configuration file has been found.
        /// </summary>
        public virtual bool HasConfig(ConfigurationLevel level)
        {
            using (ConfigurationSafeHandle snapshot = Snapshot())
            using (ConfigurationSafeHandle handle = RetrieveConfigurationHandle(level, false, snapshot))
            {
                return handle != null;
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// Saves any open configuration files.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Unset a configuration variable (key and value) in the local configuration.
        /// </summary>
        /// <param name="key">The key to unset.</param>
        public virtual void Unset(string key)
        {
            Unset(key, ConfigurationLevel.Local);
        }

        /// <summary>
        /// Unset a configuration variable (key and value).
        /// </summary>
        /// <param name="key">The key to unset.</param>
        /// <param name="level">The configuration file which should be considered as the target of this operation</param>
        public virtual void Unset(string key, ConfigurationLevel level)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            using (ConfigurationSafeHandle h = RetrieveConfigurationHandle(level, true, configHandle))
            {
                Proxy.git_config_delete(h, key);
            }
        }

        internal void UnsetMultivar(string key, ConfigurationLevel level)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            using (ConfigurationSafeHandle h = RetrieveConfigurationHandle(level, true, configHandle))
            {
                Proxy.git_config_delete_multivar(h, key);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            configHandle.SafeDispose();
        }

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
        /// <param name="keyParts">The key parts</param>
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        public virtual ConfigurationEntry<T> Get<T>(string[] keyParts)
        {
            Ensure.ArgumentNotNull(keyParts, "keyParts");

            return Get<T>(string.Join(".", keyParts));
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
        /// <param name="firstKeyPart">The first key part</param>
        /// <param name="secondKeyPart">The second key part</param>
        /// <param name="thirdKeyPart">The third key part</param>
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        public virtual ConfigurationEntry<T> Get<T>(string firstKeyPart, string secondKeyPart, string thirdKeyPart)
        {
            Ensure.ArgumentNotNullOrEmptyString(firstKeyPart, "firstKeyPart");
            Ensure.ArgumentNotNullOrEmptyString(secondKeyPart, "secondKeyPart");
            Ensure.ArgumentNotNullOrEmptyString(thirdKeyPart, "thirdKeyPart");

            return Get<T>(new[] { firstKeyPart, secondKeyPart, thirdKeyPart });
        }

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
        ///   bool isBare = repo.Config.Get&lt;bool&gt;("core.bare").Value;
        ///   </code>
        /// </para>
        /// </summary>
        /// <typeparam name="T">The configuration value type</typeparam>
        /// <param name="key">The key</param>
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        public virtual ConfigurationEntry<T> Get<T>(string key)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            using (ConfigurationSafeHandle snapshot = Snapshot())
            {
                return Proxy.git_config_get_entry<T>(snapshot, key);
            }
        }

        /// <summary>
        /// Get a configuration value for a key. Keys are in the form 'section.name'.
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
        ///   bool isBare = repo.Config.Get&lt;bool&gt;("core.bare").Value;
        ///   </code>
        /// </para>
        /// </summary>
        /// <typeparam name="T">The configuration value type</typeparam>
        /// <param name="key">The key</param>
        /// <param name="level">The configuration file into which the key should be searched for</param>
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        public virtual ConfigurationEntry<T> Get<T>(string key, ConfigurationLevel level)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            using (ConfigurationSafeHandle snapshot = Snapshot())
            using (ConfigurationSafeHandle handle = RetrieveConfigurationHandle(level, false, snapshot))
            {
                if (handle == null)
                {
                    return null;
                }

                return Proxy.git_config_get_entry<T>(handle, key);
            }
        }

        /// <summary>
        /// Get a configuration value for the given key.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="key">The key</param>
        /// <returns>The configuration value, or the default value for the selected <see typeparamref="T"/>if not found</returns>
        public virtual T GetValueOrDefault<T>(string key)
        {
            return ValueOrDefault(Get<T>(key), default(T));
        }

        /// <summary>
        /// Get a configuration value for the given key,
        /// or <paramref name="defaultValue" /> if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="key">The key</param>
        /// <param name="defaultValue">The default value if the key is not set.</param>
        /// <returns>The configuration value, or the default value</returns>
        public virtual T GetValueOrDefault<T>(string key, T defaultValue)
        {
            return ValueOrDefault(Get<T>(key), defaultValue);
        }

        /// <summary>
        /// Get a configuration value for the given key
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="level">The configuration file into which the key should be searched for.</param>
        /// <returns>The configuration value, or the default value for <see typeparamref="T"/> if not found</returns>
        public virtual T GetValueOrDefault<T>(string key, ConfigurationLevel level)
        {
            return ValueOrDefault(Get<T>(key, level), default(T));
        }

        /// <summary>
        /// Get a configuration value for the given key,
        /// or <paramref name="defaultValue" /> if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="level">The configuration file into which the key should be searched for.</param>
        /// <param name="defaultValue">The selector used to generate a default value if the key is not set.</param>
        /// <returns>The configuration value, or the default value.</returns>
        public virtual T GetValueOrDefault<T>(string key, ConfigurationLevel level, T defaultValue)
        {
            return ValueOrDefault(Get<T>(key, level), defaultValue);
        }

        /// <summary>
        /// Get a configuration value for the given key parts
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="keyParts">The key parts.</param>
        /// <returns>The configuration value, or the default value for<see typeparamref="T"/> if not found</returns>
        public virtual T GetValueOrDefault<T>(string[] keyParts)
        {
            return ValueOrDefault(Get<T>(keyParts), default(T));
        }

        /// <summary>
        /// Get a configuration value for the given key parts,
        /// or <paramref name="defaultValue" /> if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="keyParts">The key parts.</param>
        /// <param name="defaultValue">The default value if the key is not set.</param>
        /// <returns>The configuration value, or the default value.</returns>
        public virtual T GetValueOrDefault<T>(string[] keyParts, T defaultValue)
        {
            return ValueOrDefault(Get<T>(keyParts), defaultValue);
        }

        /// <summary>
        /// Get a configuration value for the given key parts.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="firstKeyPart">The first key part.</param>
        /// <param name="secondKeyPart">The second key part.</param>
        /// <param name="thirdKeyPart">The third key part.</param>
        /// <returns>The configuration value, or the default value for the selected <see typeparamref="T"/> if not found</returns>
        public virtual T GetValueOrDefault<T>(string firstKeyPart, string secondKeyPart, string thirdKeyPart)
        {
            return ValueOrDefault(Get<T>(firstKeyPart, secondKeyPart, thirdKeyPart), default(T));
        }

        /// <summary>
        /// Get a configuration value for the given key parts,
        /// or <paramref name="defaultValue" /> if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="firstKeyPart">The first key part.</param>
        /// <param name="secondKeyPart">The second key part.</param>
        /// <param name="thirdKeyPart">The third key part.</param>
        /// <param name="defaultValue">The default value if the key is not set.</param>
        /// <returns>The configuration value, or the default.</returns>
        public virtual T GetValueOrDefault<T>(string firstKeyPart, string secondKeyPart, string thirdKeyPart, T defaultValue)
        {
            return ValueOrDefault(Get<T>(firstKeyPart, secondKeyPart, thirdKeyPart), defaultValue);
        }

        /// <summary>
        /// Get a configuration value for the given key,
        /// or a value generated by <paramref name="defaultValueSelector" />
        /// if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="key">The key</param>
        /// <param name="defaultValueSelector">The selector used to generate a default value if the key is not set.</param>
        /// <returns>The configuration value, or a generated default.</returns>
        public virtual T GetValueOrDefault<T>(string key, Func<T> defaultValueSelector)
        {
            return ValueOrDefault(Get<T>(key), defaultValueSelector);
        }

        /// <summary>
        /// Get a configuration value for the given key,
        /// or a value generated by <paramref name="defaultValueSelector" />
        /// if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="level">The configuration file into which the key should be searched for.</param>
        /// <param name="defaultValueSelector">The selector used to generate a default value if the key is not set.</param>
        /// <returns>The configuration value, or a generated default.</returns>
        public virtual T GetValueOrDefault<T>(string key, ConfigurationLevel level, Func<T> defaultValueSelector)
        {
            return ValueOrDefault(Get<T>(key, level), defaultValueSelector);
        }

        /// <summary>
        /// Get a configuration value for the given key parts,
        /// or a value generated by <paramref name="defaultValueSelector" />
        /// if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="keyParts">The key parts.</param>
        /// <param name="defaultValueSelector">The selector used to generate a default value if the key is not set.</param>
        /// <returns>The configuration value, or a generated default.</returns>
        public virtual T GetValueOrDefault<T>(string[] keyParts, Func<T> defaultValueSelector)
        {
            return ValueOrDefault(Get<T>(keyParts), defaultValueSelector);
        }

        /// <summary>
        /// Get a configuration value for the given key parts,
        /// or a value generated by <paramref name="defaultValueSelector" />
        /// if the key is not set.
        /// </summary>
        /// <typeparam name="T">The configuration value type.</typeparam>
        /// <param name="firstKeyPart">The first key part.</param>
        /// <param name="secondKeyPart">The second key part.</param>
        /// <param name="thirdKeyPart">The third key part.</param>
        /// <param name="defaultValueSelector">The selector used to generate a default value if the key is not set.</param>
        /// <returns>The configuration value, or a generated default.</returns>
        public virtual T GetValueOrDefault<T>(string firstKeyPart, string secondKeyPart, string thirdKeyPart, Func<T> defaultValueSelector)
        {
            return ValueOrDefault(Get<T>(firstKeyPart, secondKeyPart, thirdKeyPart), defaultValueSelector);
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

        /// <summary>
        /// Set a configuration value for a key in the local configuration. Keys are in the form 'section.name'.
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
        public virtual void Set<T>(string key, T value)
        {
            Set(key, value, ConfigurationLevel.Local);
        }

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
        public virtual void Set<T>(string key, T value, ConfigurationLevel level)
        {
            Ensure.ArgumentNotNull(value, "value");
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            using (ConfigurationSafeHandle h = RetrieveConfigurationHandle(level, true, configHandle))
            {
                if (!configurationTypedUpdater.ContainsKey(typeof(T)))
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Generic Argument of type '{0}' is not supported.", typeof(T).FullName));
                }

                configurationTypedUpdater[typeof(T)](key, value, h);
            }
        }

        /// <summary>
        /// Find configuration entries matching <paramref name="regexp"/>.
        /// </summary>
        /// <param name="regexp">A regular expression.</param>
        /// <returns>Matching entries.</returns>
        public virtual IEnumerable<ConfigurationEntry<string>> Find(string regexp)
        {
            return Find(regexp, ConfigurationLevel.Local);
        }

        /// <summary>
        /// Find configuration entries matching <paramref name="regexp"/>.
        /// </summary>
        /// <param name="regexp">A regular expression.</param>
        /// <param name="level">The configuration file into which the key should be searched for.</param>
        /// <returns>Matching entries.</returns>
        public virtual IEnumerable<ConfigurationEntry<string>> Find(string regexp, ConfigurationLevel level)
        {
            Ensure.ArgumentNotNullOrEmptyString(regexp, "regexp");

            using (ConfigurationSafeHandle snapshot = Snapshot())
            using (ConfigurationSafeHandle h = RetrieveConfigurationHandle(level, true, snapshot))
            {
                return Proxy.git_config_iterator_glob(h, regexp, BuildConfigEntry).ToList();
            }
        }

        private ConfigurationSafeHandle RetrieveConfigurationHandle(ConfigurationLevel level, bool throwIfStoreHasNotBeenFound, ConfigurationSafeHandle fromHandle)
        {
            ConfigurationSafeHandle handle = null;
            if (fromHandle != null)
            {
                handle = Proxy.git_config_open_level(fromHandle, level);
            }

            if (handle == null && throwIfStoreHasNotBeenFound)
            {
                throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture,
                                                              "No {0} configuration file has been found.",
                                                              Enum.GetName(typeof(ConfigurationLevel), level)));
            }

            return handle;
        }

        private static Action<string, object, ConfigurationSafeHandle> GetUpdater<T>(Action<ConfigurationSafeHandle, string, T> setter)
        {
            return (key, val, handle) => setter(handle, key, (T)val);
        }

        private readonly static IDictionary<Type, Action<string, object, ConfigurationSafeHandle>> configurationTypedUpdater = new Dictionary<Type, Action<string, object, ConfigurationSafeHandle>>
        {
            { typeof(int), GetUpdater<int>(Proxy.git_config_set_int32) },
            { typeof(long), GetUpdater<long>(Proxy.git_config_set_int64) },
            { typeof(bool), GetUpdater<bool>(Proxy.git_config_set_bool) },
            { typeof(string), GetUpdater<string>(Proxy.git_config_set_string) },
        };

        /// <summary>
        /// Returns an enumerator that iterates through the configuration entries.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the configuration entries.</returns>
        public virtual IEnumerator<ConfigurationEntry<string>> GetEnumerator()
        {
            return BuildConfigEntries().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ConfigurationEntry<string>>)this).GetEnumerator();
        }

        private IEnumerable<ConfigurationEntry<string>> BuildConfigEntries()
        {
            return Proxy.git_config_foreach(configHandle, BuildConfigEntry);
        }

        private static ConfigurationEntry<string> BuildConfigEntry(IntPtr entryPtr)
        {
            var entry = entryPtr.MarshalAs<GitConfigEntry>();

            return new ConfigurationEntry<string>(LaxUtf8Marshaler.FromNative(entry.namePtr),
                                                  LaxUtf8Marshaler.FromNative(entry.valuePtr),
                                                  (ConfigurationLevel)entry.level);
        }

        /// <summary>
        /// Builds a <see cref="Signature"/> based on current configuration. If it is not found or
        /// some configuration is missing, <code>null</code> is returned.
        /// <para>
        ///    The same escalation logic than in git.git will be used when looking for the key in the config files:
        ///       - local: the Git file in the current repository
        ///       - global: the Git file specific to the current interactive user (usually in `$HOME/.gitconfig`)
        ///       - xdg: another Git file specific to the current interactive user (usually in `$HOME/.config/git/config`)
        ///       - system: the system-wide Git file
        /// </para>
        /// </summary>
        /// <param name="now">The timestamp to use for the <see cref="Signature"/>.</param>
        /// <returns>The signature or null if no user identity can be found in the configuration.</returns>
        public virtual Signature BuildSignature(DateTimeOffset now)
        {
            var name = this.GetValueOrDefault<string>("user.name");
            var email = this.GetValueOrDefault<string>("user.email");

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
            {
                return null;
            }

            return new Signature(name, email, now);
        }

        internal Signature BuildSignatureOrThrow(DateTimeOffset now)
        {
            var signature = BuildSignature(now);
            if (signature == null)
            {
                throw new LibGit2SharpException("This overload requires 'user.name' and 'user.email' to be set. " +
                                                "Use a different overload or set those variables in the configuation");
            }

            return signature;
        }

        private ConfigurationSafeHandle Snapshot()
        {
            return Proxy.git_config_snapshot(configHandle);
        }
    }
}
