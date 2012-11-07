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
                Proxy.git_config_add_file_ondisk(localHandle, repoConfigLocation, (uint)ConfigurationLevel.Local);

                if (globalConfigPath != null)
                {
                    Proxy.git_config_add_file_ondisk(localHandle, globalConfigPath, (uint)ConfigurationLevel.Global);
                }

                if (systemConfigPath != null)
                {
                    Proxy.git_config_add_file_ondisk(localHandle, systemConfigPath, (uint)ConfigurationLevel.System);
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
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

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
        [Obsolete("This method will be removed in the next release. Please use a different overload instead.")]
        public virtual T Get<T>(string key, T defaultValue)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            var val = Get<T>(key);

            return val == null ? defaultValue : val.Value;
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
        [Obsolete("This method will be removed in the next release. Please use a different overload instead.")]        
        public virtual T Get<T>(string firstKeyPart, string secondKeyPart, T defaultValue)
        {
            Ensure.ArgumentNotNullOrEmptyString(firstKeyPart, "firstKeyPart");
            Ensure.ArgumentNotNullOrEmptyString(secondKeyPart, "secondKeyPart");

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
        [Obsolete("This method will be removed in the next release. Please use a different overload instead.")]
        public virtual T Get<T>(string firstKeyPart, string secondKeyPart, string thirdKeyPart, T defaultValue)
        {
            Ensure.ArgumentNotNullOrEmptyString(firstKeyPart, "firstKeyPart");
            Ensure.ArgumentNotNullOrEmptyString(secondKeyPart, "secondKeyPart");
            Ensure.ArgumentNotNullOrEmptyString(thirdKeyPart, "secondKeyPart");

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
        [Obsolete("This method will be removed in the next release. Please use a different overload instead.")]
        public virtual T Get<T>(string[] keyParts, T defaultValue)
        {
            Ensure.ArgumentNotNull(keyParts, "keyParts");

            return Get(string.Join(".", keyParts), defaultValue);
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
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        public ConfigurationEntry<T> Get<T>(string key)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            ConfigurationSafeHandle handle = (localHandle ?? globalHandle) ?? systemHandle;

            if (handle == null)
            {
                throw new LibGit2SharpException("Could not find a local, global or system level configuration.");
            }

            return Proxy.git_config_get_config_entry<T>(handle, key);
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
        ///     bool isBare = repo.Config.Get&lt;bool&gt;(new []{ "core", "bare" }).Value;
        ///     </code>
        ///   </para>
        /// </summary>
        /// <typeparam name = "T">The configuration value type</typeparam>
        /// <param name = "keyParts">The key parts</param>
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        public ConfigurationEntry<T> Get<T>(string[] keyParts)
        {
            Ensure.ArgumentNotNull(keyParts, "keyParts");

            return Get<T>(string.Join(".", keyParts));
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
        ///     string where = repo.Config.Get&lt;string&gt;("difftool", "kdiff3", "path").Value;
        ///     </code>
        ///   </para>
        /// </summary>
        /// <typeparam name = "T">The configuration value type</typeparam>
        /// <param name = "firstKeyPart">The first key part</param>
        /// <param name = "secondKeyPart">The second key part</param>
        /// <param name = "thirdKeyPart">The third key part</param>
        /// <returns>The <see cref="ConfigurationEntry{T}"/>, or null if not set</returns>
        public ConfigurationEntry<T> Get<T>(string firstKeyPart, string secondKeyPart, string thirdKeyPart)
        {
            Ensure.ArgumentNotNullOrEmptyString(firstKeyPart, "firstKeyPart");
            Ensure.ArgumentNotNullOrEmptyString(secondKeyPart, "secondKeyPart");
            Ensure.ArgumentNotNullOrEmptyString(thirdKeyPart, "secondKeyPart");

            return Get<T>(new[] { firstKeyPart, secondKeyPart, thirdKeyPart });
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
        /// <param name = "value">The value</param>
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
            return Proxy.git_config_foreach(localHandle, entryPtr =>
            {
                var entry = (GitConfigEntry)Marshal.PtrToStructure(entryPtr, typeof(GitConfigEntry));

                return new ConfigurationEntry(Utf8Marshaler.FromNative(entry.namePtr),
                                              Utf8Marshaler.FromNative(entry.valuePtr),
                                              (ConfigurationLevel)entry.level);
            });
        }
    }
}
