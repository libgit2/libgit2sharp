using System;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides access to configuration variables for a repository.
    /// </summary>
    public class Configuration : IDisposable
    {
        private readonly string globalGitConfig;
        private readonly string homeDirectory;

        private readonly Repository repository;
        private ConfigurationSafeHandle globalHandle;
        private ConfigurationSafeHandle localHandle;

        public Configuration(Repository repository, string userConfigFile = null)
        {
            this.repository = repository;
            if (userConfigFile == null)
            {
                homeDirectory = (Environment.OSVersion.Platform == PlatformID.Unix ||
                                 Environment.OSVersion.Platform == PlatformID.MacOSX)
                                    ? Environment.GetEnvironmentVariable("HOME")
                                    : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
                if (homeDirectory != null)
                    globalGitConfig = Path.Combine(homeDirectory, ".gitconfig");
            }
            else
            {
                globalGitConfig = new FileInfo(userConfigFile).FullName;
            }
            Init();
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
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (localHandle != null && !localHandle.IsInvalid)
            {
                localHandle.Dispose();
            }
            if (globalHandle != null && !globalHandle.IsInvalid)
            {
                globalHandle.Dispose();
            }
        }

        ///<summary>
        ///  Get a configuration value for a key. Keys are in the form 'section.name'.
        ///  <para>
        ///    For example in  order to get the value for this in a .git\config file:
        ///
        ///    [core]
        ///    bare = true
        ///
        ///    You would call:
        ///
        ///    bool isBare = repo.Config.Get&lt;bool&gt;("core.bare");
        ///  </para>
        ///</summary>
        ///<typeparam name = "T"></typeparam>
        ///<param name = "key"></param>
        ///<returns></returns>
        public T Get<T>(string key)
        {
            if (typeof (T) == typeof (string)) return (T) (object) GetString(key);
            if (typeof (T) == typeof (bool)) return (T) (object) GetBool(key);
            if (typeof (T) == typeof (int)) return (T) (object) GetInt(key);
            if (typeof (T) == typeof (long)) return (T) (object) GetLong(key);

            return default(T);
        }

        private bool GetBool(string key)
        {
            bool value;
            Ensure.Success(NativeMethods.git_config_get_bool(localHandle, key, out value));
            return value;
        }

        private int GetInt(string key)
        {
            int value;
            Ensure.Success(NativeMethods.git_config_get_int(localHandle, key, out value));
            return value;
        }

        private long GetLong(string key)
        {
            long value;
            Ensure.Success(NativeMethods.git_config_get_long(localHandle, key, out value));
            return value;
        }

        private string GetString(string key)
        {
            IntPtr value;
            Ensure.Success(NativeMethods.git_config_get_string(localHandle, key, out value));
            return value.MarshallAsString();
        }

        private void Init()
        {
            Ensure.Success(NativeMethods.git_repository_config(out localHandle, repository.Handle, globalGitConfig, null));
            Ensure.Success(NativeMethods.git_config_open_global(out globalHandle));
        }

        public void Save()
        {
            Dispose(true);
            Init();
        }

        ///<summary>
        ///  Set a configuration value for a key. Keys are in the form 'section.name'.
        ///  <para>
        ///    For example in order to set the value for this in a .git\config file:
        ///
        ///    [test]
        ///    boolsetting = true
        ///
        ///    You would call:
        ///
        ///    repo.Config.Set("test.boolsetting", true);
        ///  </para>
        ///</summary>
        ///<typeparam name = "T"></typeparam>
        ///<param name = "key"></param>
        ///<param name = "value"></param>
        ///<param name = "level"></param>
        public void Set<T>(string key, T value, ConfigurationLevel level = ConfigurationLevel.Local)
        {
            var h = level == ConfigurationLevel.Local ? localHandle : globalHandle;

            if (typeof (T) == typeof (string))
            {
                Ensure.Success(NativeMethods.git_config_set_string(h, key, (string) (object) value));
            }

            if (typeof (T) == typeof (bool))
            {
                Ensure.Success(NativeMethods.git_config_set_bool(h, key, (bool) (object) value));
            }

            if (typeof (T) == typeof (int))
            {
                Ensure.Success(NativeMethods.git_config_set_int(h, key, (int) (object) value));
            }

            if (typeof (T) == typeof (long))
            {
                Ensure.Success(NativeMethods.git_config_set_long(h, key, (long) (object) value));
            }
        }
    }
}