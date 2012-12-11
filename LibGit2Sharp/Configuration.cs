using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides access to configuration variables for a repository.
    /// </summary>
    public class Configuration : IDisposable,
        IEnumerable<ConfigurationEntry>,
        IEnumerable<ConfigurationEntry<string>>
    {
        private readonly FilePath globalConfigPath;
        private readonly FilePath xdgConfigPath;
        private readonly FilePath systemConfigPath;

        private readonly Repository repository;

        private ConfigurationSafeHandle configHandle;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Configuration()
        { }

        internal Configuration(Repository repository, string globalConfigurationFileLocation,
            string xdgConfigurationFileLocation, string systemConfigurationFileLocation)
        {
            this.repository = repository;

            globalConfigPath = globalConfigurationFileLocation ?? Proxy.git_config_find_global();
            xdgConfigPath = xdgConfigurationFileLocation ?? Proxy.git_config_find_xdg();
            systemConfigPath = systemConfigurationFileLocation ?? Proxy.git_config_find_system();

            Init();
        }

        private void Init()
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

        /// <summary>
        ///   Access configuration values without a repository. Generally you want to access configuration via an instance of <see cref = "Repository" /> instead.
        /// </summary>
        /// <param name="globalConfigurationFileLocation">Path to a Global configuration file. If null, the default path for a global configuration file will be probed.</param>
        /// <param name="xdgConfigurationFileLocation">Path to a XDG configuration file. If null, the default path for a XDG configuration file will be probed.</param>
        /// <param name="systemConfigurationFileLocation">Path to a System configuration file. If null, the default path for a system configuration file will be probed.</param>
        public Configuration(string globalConfigurationFileLocation = null, string xdgConfigurationFileLocation = null, string systemConfigurationFileLocation = null)
            : this(null, globalConfigurationFileLocation, xdgConfigurationFileLocation, systemConfigurationFileLocation)
        {
        }

        /// <summary>
        ///   Determines if a Git configuration file specific to the current interactive user has been found.
        /// </summary>
        [Obsolete("This property will be removed in the next release. Please use HasConfig() instead.")]
        public virtual bool HasGlobalConfig
        {
            get { return HasConfig(ConfigurationLevel.Global); }
        }

        /// <summary>
        ///   Determines if a system-wide Git configuration file has been found.
        /// </summary>
        [Obsolete("This property will be removed in the next release. Please use HasConfig() instead.")]
        public virtual bool HasSystemConfig
        {
            get { return HasConfig(ConfigurationLevel.System); }
        }

        /// <summary>
        ///   Determines which configuration file has been found.
        /// </summary>
        public virtual bool HasConfig(ConfigurationLevel level)
        {
            using (ConfigurationSafeHandle handle = RetrieveConfigurationHandle(level, false))
            {
                return handle != null;
            }
        }

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        ///   Saves any open configuration files.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        ///   Unset a configuration variable (key and value).
        /// </summary>
        /// <param name = "key">The key to unset.</param>
        /// <param name = "level">The configuration file which should be considered as the target of this operation</param>
        public virtual void Unset(string key, ConfigurationLevel level = ConfigurationLevel.Local)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            using (ConfigurationSafeHandle h = RetrieveConfigurationHandle(level, true))
            {
                Proxy.git_config_delete(h, key);
            }
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            configHandle.SafeDispose();
        }

        /// <summary>
        ///   Get a configuration value for a key. Keys are in the form 'section.name'.
        ///   <para>
        ///      The same escalation logic than in git.git will be used when looking for the key in the config files:
        ///         - local: the Git file in the current repository
        ///         - global: the Git file specific to the current interactive user (usually in `$HOME/.gitconfig`)
        ///         - xdg: another Git file specific to the current interactive user (usually in `$HOME/.config/git/config`)
        ///         - system: the system-wide Git file
        ///
        ///     The first occurence of the key will be returned.
        ///   </para>
        ///   <para>
        ///     For example in  order to get the value for this in a .git\config file:
        /// 
        ///     <code>
        ///     [core]
        ///     bare = true
        ///     </code>
        /// 
        ///     You would call:
        /// 
        ///     <code>
        ///     bool isBare = repo.Config.Get&lt;bool&gt;("core.bare").Value;
        ///     </code>
        ///   </para>
        /// </summary>
        /// <typeparam name = "T">The configuration value type</typeparam>
        /// <param name = "key">The key</param>
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        public ConfigurationEntry<T> Get<T>(string key)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            return Proxy.git_config_get_entry<T>(configHandle, key);
        }

        /// <summary>
        ///   Get a configuration value for a key. Keys are in the form 'section.name'.
        ///   <para>
        ///     For example in  order to get the value for this in a .git\config file:
        ///
        ///     <code>
        ///     [core]
        ///     bare = true
        ///     </code>
        ///
        ///     You would call:
        ///
        ///     <code>
        ///     bool isBare = repo.Config.Get&lt;bool&gt;("core.bare").Value;
        ///     </code>
        ///   </para>
        /// </summary>
        /// <typeparam name = "T">The configuration value type</typeparam>
        /// <param name = "key">The key</param>
        /// <param name = "level">The configuration file into which the key should be searched for</param>
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        public ConfigurationEntry<T> Get<T>(string key, ConfigurationLevel level)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            using (ConfigurationSafeHandle handle = RetrieveConfigurationHandle(level, false))
            {
                if (handle == null)
                {
                    return null;
                }

                return Proxy.git_config_get_entry<T>(handle, key);
            }
        }

        /// <summary>
        ///   Set a configuration value for a key. Keys are in the form 'section.name'.
        ///   <para>
        ///     For example in order to set the value for this in a .git\config file:
        ///   
        ///     [test]
        ///     boolsetting = true
        ///   
        ///     You would call:
        ///   
        ///     repo.Config.Set("test.boolsetting", true);
        ///   </para>
        /// </summary>
        /// <typeparam name = "T">The configuration value type</typeparam>
        /// <param name = "key">The key parts</param>
        /// <param name = "value">The value</param>
        /// <param name = "level">The configuration file which should be considered as the target of this operation</param>
        public virtual void Set<T>(string key, T value, ConfigurationLevel level = ConfigurationLevel.Local)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            using (ConfigurationSafeHandle h = RetrieveConfigurationHandle(level, true))
            {
                if (!configurationTypedUpdater.ContainsKey(typeof(T)))
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Generic Argument of type '{0}' is not supported.", typeof(T).FullName));
                }

                configurationTypedUpdater[typeof(T)](key, value, h);
            }
        }

        private ConfigurationSafeHandle RetrieveConfigurationHandle(ConfigurationLevel level, bool throwIfStoreHasNotBeenFound)
        {
            ConfigurationSafeHandle handle = null;
            if (configHandle != null)
            {
                handle = Proxy.git_config_open_level(configHandle, level);
            }

            if (handle == null && throwIfStoreHasNotBeenFound)
            {
                throw new LibGit2SharpException("No matching configuration file has been found.");
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

        IEnumerator<ConfigurationEntry<string>> IEnumerable<ConfigurationEntry<String>>.GetEnumerator()
        {
            return BuildConfigEntries().Cast<ConfigurationEntry<string>>().GetEnumerator();
        }

        [Obsolete("This method will be removed in the next release. Please use a different overload instead.")]
        IEnumerator<ConfigurationEntry> IEnumerable<ConfigurationEntry>.GetEnumerator()
        {
            return BuildConfigEntries().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ConfigurationEntry<string>>)this).GetEnumerator();
        }

        private ICollection<ConfigurationEntry> BuildConfigEntries()
        {
            return Proxy.git_config_foreach(configHandle, entryPtr =>
            {
                var entry = (GitConfigEntry)Marshal.PtrToStructure(entryPtr, typeof(GitConfigEntry));

                return new ConfigurationEntry(Utf8Marshaler.FromNative(entry.namePtr),
                                              Utf8Marshaler.FromNative(entry.valuePtr),
                                              (ConfigurationLevel)entry.level);
            });
        }
    }
}
