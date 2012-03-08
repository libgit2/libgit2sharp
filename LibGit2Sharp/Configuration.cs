using System;
using System.Collections.Generic;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides access to configuration variables for a repository.
    /// </summary>
    public class Configuration : IConfiguration
    {
        private readonly string globalConfigPath;
        private readonly string systemConfigPath;

        private readonly Repository repository;

        private ConfigurationSafeHandle systemHandle;
        private ConfigurationSafeHandle globalHandle;
        private ConfigurationSafeHandle localHandle;

        internal Configuration(Repository repository)
        {
            this.repository = repository;

            globalConfigPath = ConvertPath(NativeMethods.git_config_find_global);
            systemConfigPath = ConvertPath(NativeMethods.git_config_find_system);

            Init();
        }

        /// <summary>
        ///   Access configuration values without a repository. Generally you want to access configuration via an instance of <see cref = "Repository" /> instead.
        /// </summary>
        public Configuration()
            : this(null)
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
        public bool HasGlobalConfig
        {
            get { return globalConfigPath != null; }
        }

        /// <summary>
        ///   Determines if a system-wide Git configuration file has been found.
        /// </summary>
        public bool HasSystemConfig
        {
            get { return systemConfigPath != null; }
        }

        private static string ConvertPath(Func<byte[], int> pathRetriever)
        {
            var buffer = new byte[NativeMethods.GIT_PATH_MAX];

            int result = pathRetriever(buffer);

            //TODO: Make libgit2 return different codes to clearly identify a not found file (GIT_ENOTFOUND ) from any other error (!= GIT_SUCCESS)
            if (result != (int)GitErrorCode.GIT_SUCCESS)
            {
                return null;
            }

            return Utf8Marshaler.Utf8FromBuffer(buffer);
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
        ///   Delete a configuration variable (key and value).
        /// </summary>
        /// <param name = "key">The key to delete.</param>
        public void Delete(string key)
        {
            Ensure.Success(NativeMethods.git_config_delete(localHandle, key));
            Save();
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

        private static T ProcessReadResult<T>(int res, T value, T defaultValue, Func<object, T> postProcessor = null)
        {
            if (res == (int)GitErrorCode.GIT_ENOTFOUND)
            {
                return defaultValue;
            }

            Ensure.Success(res);

            if (postProcessor == null)
            {
                return value;
            }

            return postProcessor(value);
        }

        private readonly IDictionary<Type, Func<string, object, ConfigurationSafeHandle, object>> configurationTypedRetriever = ConfigurationTypedRetriever();

        private static Dictionary<Type, Func<string, object, ConfigurationSafeHandle, object>> ConfigurationTypedRetriever()
        {
            var dic = new Dictionary<Type, Func<string, object, ConfigurationSafeHandle, object>>();

            dic.Add(typeof(int), (key, dv, handle) =>
                                     {
                                         int value;
                                         int res = NativeMethods.git_config_get_int32(handle, key, out value);
                                         return ProcessReadResult<object>(res, value, dv);
                                     });

            dic.Add(typeof(long), (key, dv, handle) =>
                                      {
                                          long value;
                                          int res = NativeMethods.git_config_get_int64(handle, key, out value);
                                          return ProcessReadResult<object>(res, value, dv);
                                      });

            dic.Add(typeof(bool), (key, dv, handle) =>
                                      {
                                          bool value;
                                          int res = NativeMethods.git_config_get_bool(handle, key, out value);
                                          return ProcessReadResult<object>(res, value, dv);
                                      });

            dic.Add(typeof(string), (key, dv, handle) =>
                                        {
                                            string value;
                                            int res = NativeMethods.git_config_get_string(handle, key, out value);
                                            return ProcessReadResult<object>(res, value, dv);
                                        });

            return dic;
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
        public T Get<T>(string key, T defaultValue)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            if (!configurationTypedRetriever.ContainsKey(typeof(T)))
            {
                throw new ArgumentException(string.Format("Generic Argument of type '{0}' is not supported.", typeof(T).FullName));
            }

            ConfigurationSafeHandle handle = (LocalHandle ?? globalHandle) ?? systemHandle;
            if (handle == null)
            {
                throw new LibGit2Exception("Could not find a local, global or system level configuration.");
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
        public T Get<T>(string firstKeyPart, string secondKeyPart, T defaultValue)
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
        public T Get<T>(string firstKeyPart, string secondKeyPart, string thirdKeyPart, T defaultValue)
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
        public T Get<T>(string[] keyParts, T defaultValue)
        {
            Ensure.ArgumentNotNull(keyParts, "keyParts");

            return Get(string.Join(".", keyParts), defaultValue);
        }

        private void Init()
        {
            if (repository != null)
            {
                Ensure.Success(NativeMethods.git_repository_config(out localHandle, repository.Handle));
            }

            if (globalConfigPath != null)
            {
                Ensure.Success(NativeMethods.git_config_open_ondisk(out globalHandle, globalConfigPath));
            }

            if (systemConfigPath != null)
            {
                Ensure.Success(NativeMethods.git_config_open_ondisk(out systemHandle, systemConfigPath));
            }
        }

        private void Save()
        {
            Dispose(true);
            Init();
        }

        private readonly IDictionary<Type, Action<string, object, ConfigurationSafeHandle>> configurationTypedUpdater = ConfigurationTypedUpdater();

        private static Dictionary<Type, Action<string, object, ConfigurationSafeHandle>> ConfigurationTypedUpdater()
        {
            var dic = new Dictionary<Type, Action<string, object, ConfigurationSafeHandle>>();

            dic.Add(typeof(int), (key, val, handle) => Ensure.Success(NativeMethods.git_config_set_int32(handle, key, (int)val)));
            dic.Add(typeof(long), (key, val, handle) => Ensure.Success(NativeMethods.git_config_set_int64(handle, key, (long)val)));
            dic.Add(typeof(bool), (key, val, handle) => Ensure.Success(NativeMethods.git_config_set_bool(handle, key, (bool)val)));
            dic.Add(typeof(string), (key, val, handle) => Ensure.Success(NativeMethods.git_config_set_string(handle, key, (string)val)));

            return dic;
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
        /// <typeparam name = "T"></typeparam>
        /// <param name = "key"></param>
        /// <param name = "value"></param>
        /// <param name = "level"></param>
        public void Set<T>(string key, T value, ConfigurationLevel level = ConfigurationLevel.Local)
        {
            Ensure.ArgumentNotNullOrEmptyString(key, "key");

            if (level == ConfigurationLevel.Local && !HasLocalConfig)
            {
                throw new LibGit2Exception("No local configuration file has been found. You must use ConfigurationLevel.Global when accessing configuration outside of repository.");
            }

            if (level == ConfigurationLevel.Global && !HasGlobalConfig)
            {
                throw new LibGit2Exception("No global configuration file has been found.");
            }

            if (level == ConfigurationLevel.System && !HasSystemConfig)
            {
                throw new LibGit2Exception("No system configuration file has been found.");
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
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Configuration level has an unexpected value ('{0}').", Enum.GetName(typeof(ConfigurationLevel), level)), "level");
            }

            if (!configurationTypedUpdater.ContainsKey(typeof(T)))
            {
                throw new ArgumentException(string.Format("Generic Argument of type '{0}' is not supported.", typeof(T).FullName));
            }

            configurationTypedUpdater[typeof(T)](key, value, h);
            Save();
        }
    }
}
