using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides access to the '.gitconfig' configuration for a repository.
    /// </summary>
    public class Configuration : IDisposable
    {
        private readonly ConfigurationSafeHandle handle;
        private readonly Repository repository;

        public Configuration(Repository repository)
        {
            this.repository = repository;
            Ensure.Success(NativeMethods.git_repository_config(out handle, this.repository.Handle, null, null));
        }

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (handle != null && !handle.IsInvalid)
            {
                handle.Dispose();
            }
        }

        /// <summary>
        ///   Get a configuration value for a key. Keys are in the form 'section.name'. For example in
        ///   order to get the value for this in a .gitconfig file:
        /// 
        ///   [core]
        ///   bare = true
        /// 
        ///   You would call:
        /// 
        ///   bool isBare = repo.Config.Get<bool>("core.bare");
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "key"></param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)GetString(key);
            }

            if (typeof(T) == typeof(bool))
            {
                return (T)(object)GetBool(key);
            }

            if (typeof(T) == typeof(int))
            {
                return (T)(object)GetInt(key);
            }

            if (typeof(T) == typeof(long))
            {
                return (T)(object)GetLong(key);
            }

            return default(T);
        }

        private bool GetBool(string key)
        {
            bool value;
            Ensure.Success(NativeMethods.git_config_get_bool(handle, key, out value));
            return value;
        }

        private int GetInt(string key)
        {
            int value;
            Ensure.Success(NativeMethods.git_config_get_int(handle, key, out value));
            return value;
        }

        private long GetLong(string key)
        {
            long value;
            Ensure.Success(NativeMethods.git_config_get_long(handle, key, out value));
            return value;
        }

        private string GetString(string key)
        {
            IntPtr value;
            Ensure.Success(NativeMethods.git_config_get_string(handle, key, out value));
            return value.MarshallAsString();
        }
    }
}
