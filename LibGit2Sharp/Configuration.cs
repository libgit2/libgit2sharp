using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides access to configuration variables for a repository.
    /// </summary>
    public class Configuration : IDisposable, IEnumerable<ConfigurationEntry>
    {
        private readonly FilePath globalConfigPath;
        private readonly FilePath systemConfigPath;

        private readonly Repository repository;

        private ConfigurationSafeHandle systemHandle;
        private ConfigurationSafeHandle globalHandle;
        private ConfigurationSafeHandle localHandle;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Configuration()
        { }

        internal Configuration(Repository repository, string globalConfigurationFileLocation, string systemConfigurationFileLocation)
        {
            this.repository = repository;

            globalConfigPath = globalConfigurationFileLocation ?? Proxy.git_config_find_global();
            systemConfigPath = systemConfigurationFileLocation ?? Proxy.git_config_find_system();

            Init();
        }

        private void Init()
        {
            if (repository != null)
            {
                //TODO: push back this logic into libgit2. 
                // As stated by @carlosmn "having a helper function to load the defaults and then allowing you
                // to modify it before giving it to git_repository_open_ext() would be a good addition, I think."
                //  -- Agreed :)

                localHandle = Proxy.git_config_new();

                string repoConfigLocation = Path.Combine(repository.Info.Path, "config");
                Proxy.git_config_add_file_ondisk(localHandle, repoConfigLocation, 3);

                if (globalConfigPath != null)
                {
                    Proxy.git_config_add_file_ondisk(localHandle, globalConfigPath, 2);
                }

                if (systemConfigPath != null)
                {
                    Proxy.git_config_add_file_ondisk(localHandle, systemConfigPath, 1);
                }

                Proxy.git_repository_set_config(repository.Handle, localHandle);
            }

            if (globalConfigPath != null)
            {
                globalHandle = Proxy.git_config_open_ondisk(globalConfigPath);
            }

            if (systemConfigPath != null)
            {
                systemHandle = Proxy.git_config_open_ondisk(systemConfigPath);
            }
        }

        /// <summary>
        ///   Access configuration values without a repository. Generally you want to access configuration via an instance of <see cref = "Repository" /> instead.
        /// </summary>
        /// <param name="globalConfigurationFileLocation">Path to a Global configuration file. If null, the default path for a global configuration file will be probed.</param>
        /// <param name="systemConfigurationFileLocation">Path to a System configuration file. If null, the default path for a system configuration file will be probed.</param>
        public Configuration(string globalConfigurationFileLocation = null, string systemConfigurationFileLocation = null)
            : this(null, globalConfigurationFileLocation, systemConfigurationFileLocation)
        {
        }

        /// <summary>
        ///   Determines if there is a local repository level Git configuration file.
        /// </summary>
        private bool HasLocalConfig
        {
            get { return localHandle != null; }
        }

        /// <summary>
        ///   Determines if a Git configuration file specific to the current interactive user has been found.
        /// </summary>
        public virtual bool HasGlobalConfig
        {
            get { return globalConfigPath != null; }
        }

        /// <summary>
        ///   Determines if a system-wide Git configuration file has been found.
        /// </summary>
        public virtual bool HasSystemConfig
        {
            get { return systemConfigPath != null; }
        }

        internal ConfigurationSafeHandle LocalHandle
        {
            get { return localHandle; }
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
            ConfigurationSafeHandle h = RetrieveConfigurationHandle(level);

            bool success = Proxy.git_config_delete(h, key);

            if (success)
            {
                Save();
            }
        }

        /// <summary>
        ///   Delete a configuration variable (key and value).
        /// </summary>
        /// <param name = "key">The key to delete.</param>
        /// <param name = "level">The configuration file which should be considered as the target of this operation</param>
        [Obsolete("This method will be removed in the next release. Please use Unset() instead.")]
        public void Delete(string key, ConfigurationLevel level = ConfigurationLevel.Local)
        {
            Unset(key, level);
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            localHandle.SafeDispose();
            globalHandle.SafeDispose();
            systemHandle.SafeDispose();
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
        ///     bool isBare = repo.Config.Get&lt;bool&gt;("core.bare", false);
        ///     </code>
        ///   </para>
        /// </summary>
        /// <typeparam name = "T">The configuration value type</typeparam>
        /// <param name = "key">The key</param>
        /// <param name = "defaultValue">The default value</param>
        /// <returns>The configuration value, or <c>defaultValue</c> if not set</returns>
        public virtual T Get<T>(string key, T defaultValue)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            if (!configurationTypedRetriever.ContainsKey(typeof(T)))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Generic Argument of type '{0}' is not supported.", typeof(T).FullName));
            }

            ConfigurationSafeHandle handle = (LocalHandle ?? globalHandle) ?? systemHandle;
            if (handle == null)
            {
                throw new LibGit2SharpException("Could not find a local, global or system level configuration.");
            }

            return (T)configurationTypedRetriever[typeof(T)](key, defaultValue, handle);
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
        ///     bool isBare = repo.Config.Get&lt;bool&gt;("core", "bare", false);
        ///     </code>
        ///   </para>
        /// </summary>
        /// <typeparam name = "T">The configuration value type</typeparam>
        /// <param name = "firstKeyPart">The first key part</param>
        /// <param name = "secondKeyPart">The second key part</param>
        /// <param name = "defaultValue">The default value</param>
        /// <returns>The configuration value, or <c>defaultValue</c> if not set</returns>
        public virtual T Get<T>(string firstKeyPart, string secondKeyPart, T defaultValue)
        {
            Ensure.ArgumentNotNull(firstKeyPart, "firstKeyPart");
            Ensure.ArgumentNotNull(secondKeyPart, "secondKeyPart");

            return Get(new[] { firstKeyPart, secondKeyPart }, defaultValue);
        }

        /// <summary>
        ///   Get a configuration value for the given key parts.
        ///   <para>
        ///     For example in order to get the value for this in a .git\config file:
        ///
        ///     <code>
        ///     [difftool "kdiff3"]
        ///       path = c:/Program Files/KDiff3/kdiff3.exe
        ///     </code>
        ///
        ///     You would call:
        ///
        ///     <code>
        ///     string where = repo.Config.Get&lt;string&gt;("difftool", "kdiff3", "path", null);
        ///     </code>
        ///   </para>
        /// </summary>
        /// <typeparam name = "T">The configuration value type</typeparam>
        /// <param name = "firstKeyPart">The first key part</param>
        /// <param name = "secondKeyPart">The second key part</param>
        /// <param name = "thirdKeyPart">The third key part</param>
        /// <param name = "defaultValue">The default value</param>
        /// <returns>The configuration value, or <c>defaultValue</c> if not set</returns>
        public virtual T Get<T>(string firstKeyPart, string secondKeyPart, string thirdKeyPart, T defaultValue)
        {
            Ensure.ArgumentNotNull(firstKeyPart, "firstKeyPart");
            Ensure.ArgumentNotNull(secondKeyPart, "secondKeyPart");
            Ensure.ArgumentNotNull(thirdKeyPart, "secondKeyPart");

            return Get(new[] { firstKeyPart, secondKeyPart, thirdKeyPart }, defaultValue);
        }

        /// <summary>
        ///   Get a configuration value for the given key parts.
        ///   <para>
        ///     For example in order to get the value for this in a .git\config file:
        ///
        ///     <code>
        ///     [core]
        ///     bare = true
        ///     </code>
        ///
        ///     You would call:
        ///
        ///     <code>
        ///     bool isBare = repo.Config.Get&lt;bool&gt;(new []{ "core", "bare" }, false);
        ///     </code>
        ///   </para>
        /// </summary>
        /// <typeparam name = "T">The configuration value type</typeparam>
        /// <param name = "keyParts">The key parts</param>
        /// <param name = "defaultValue">The default value</param>
        /// <returns>The configuration value, or <c>defaultValue</c> if not set</returns>
        public virtual T Get<T>(string[] keyParts, T defaultValue)
        {
            Ensure.ArgumentNotNull(keyParts, "keyParts");

            return Get(string.Join(".", keyParts), defaultValue);
        }

        private void Save()
        {
            Dispose(true);
            Init();
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
        /// <param name = "value">The default value</param>
        /// <param name = "level">The configuration file which should be considered as the target of this operation</param>
        public virtual void Set<T>(string key, T value, ConfigurationLevel level = ConfigurationLevel.Local)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            ConfigurationSafeHandle h = RetrieveConfigurationHandle(level);

            if (!configurationTypedUpdater.ContainsKey(typeof(T)))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Generic Argument of type '{0}' is not supported.", typeof(T).FullName));
            }

            configurationTypedUpdater[typeof(T)](key, value, h);
            Save();
        }

        private ConfigurationSafeHandle RetrieveConfigurationHandle(ConfigurationLevel level)
        {
            if (level == ConfigurationLevel.Local && !HasLocalConfig)
            {
                throw new LibGit2SharpException("No local configuration file has been found. You must use ConfigurationLevel.Global when accessing configuration outside of repository.");
            }

            if (level == ConfigurationLevel.Global && !HasGlobalConfig)
            {
                throw new LibGit2SharpException("No global configuration file has been found.");
            }

            if (level == ConfigurationLevel.System && !HasSystemConfig)
            {
                throw new LibGit2SharpException("No system configuration file has been found.");
            }

            ConfigurationSafeHandle h;

            switch (level)
            {
                case ConfigurationLevel.Local:
                    h = localHandle;
                    break;

                case ConfigurationLevel.Global:
                    h = globalHandle;
                    break;

                case ConfigurationLevel.System:
                    h = systemHandle;
                    break;

                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Configuration level has an unexpected value ('{0}').", level), "level");
            }
            return h;
        }

        private static Func<string, object, ConfigurationSafeHandle, object> GetRetriever<T>(Func<ConfigurationSafeHandle, string, T> getter)
        {
            return (key, defaultValue, handle) =>
                {
                    T value = getter(handle, key);
                    if (Equals(value, default(T)))
                    {
                        return defaultValue;
                    }

                    return value;
                };
        }

        private readonly IDictionary<Type, Func<string, object, ConfigurationSafeHandle, object>> configurationTypedRetriever = new Dictionary<Type, Func<string, object, ConfigurationSafeHandle, object>>
        {
            { typeof(int), GetRetriever(Proxy.git_config_get_int32) },
            { typeof(long), GetRetriever(Proxy.git_config_get_int64) },
            { typeof(bool), GetRetriever(Proxy.git_config_get_bool) },
            { typeof(string), GetRetriever(Proxy.git_config_get_string) },
        };

        private static Action<string, object, ConfigurationSafeHandle> GetUpdater<T>(Action<ConfigurationSafeHandle, string, T> setter)
        {
            return (key, val, handle) => setter(handle, key, (T)val);
        }

        private readonly IDictionary<Type, Action<string, object, ConfigurationSafeHandle>> configurationTypedUpdater = new Dictionary<Type, Action<string, object, ConfigurationSafeHandle>>
        {
            { typeof(int), GetUpdater<int>(Proxy.git_config_set_int32) },
            { typeof(long), GetUpdater<long>(Proxy.git_config_set_int64) },
            { typeof(bool), GetUpdater<bool>(Proxy.git_config_set_bool) },
            { typeof(string), GetUpdater<string>(Proxy.git_config_set_string) },
        };

        IEnumerator<ConfigurationEntry> IEnumerable<ConfigurationEntry>.GetEnumerator()
        {
            var values = new List<ConfigurationEntry>();
            Proxy.git_config_foreach(LocalHandle, (namePtr, valuePtr, _) => {
                var name = Utf8Marshaler.FromNative(namePtr);
                var value = Utf8Marshaler.FromNative(valuePtr);
                values.Add(new ConfigurationEntry(name, value));
                return 0;
            });
            return values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ConfigurationEntry>)this).GetEnumerator();
        }
    }
}
