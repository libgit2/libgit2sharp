﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// The collection of submodules in a <see cref="Repository"/>
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class SubmoduleCollection : IEnumerable<Submodule>
    {
        internal readonly Repository repo;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected SubmoduleCollection()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2Sharp.SubmoduleCollection"/> class.
        /// </summary>
        /// <param name="repo">The repo.</param>
        internal SubmoduleCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        /// Gets the <see cref="LibGit2Sharp.Submodule"/> with the specified name.
        /// </summary>
        public virtual Submodule this[string name]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(name, nameof(name));

                return Lookup(name, handle => new Submodule(repo, name,
                                                            Proxy.git_submodule_path(handle),
                                                            Proxy.git_submodule_url(handle)));
            }
        }

        /// <summary>
        /// Initialize specified submodule.
        /// <para>
        /// Existing entries in the config file for this submodule are not be
        /// modified unless <paramref name="overwrite"/> is true.
        /// </para>
        /// </summary>
        /// <param name="name">The name of the submodule to update.</param>
        /// <param name="overwrite">Overwrite existing entries.</param>
        public virtual void Init(string name, bool overwrite)
        {
            using (var handle = Proxy.git_submodule_lookup(repo.Handle, name))
            {
                if (handle == null)
                {
                    throw new NotFoundException("Submodule lookup failed for '{0}'.",
                                                name);
                }

                Proxy.git_submodule_init(handle, overwrite);
            }
        }

        /// <summary>
        /// Update specified submodule.
        /// <para>
        ///   This will:
        ///   1) Optionally initialize the if it not already initialized,
        ///   2) clone the sub repository if it has not already been cloned, and
        ///   3) checkout the commit ID for the submodule in the sub repository.
        /// </para>
        /// </summary>
        /// <param name="name">The name of the submodule to update.</param>
        /// <param name="options">Options controlling submodule update behavior and callbacks.</param>
        public virtual void Update(string name, SubmoduleUpdateOptions options)
        {
            options ??= new SubmoduleUpdateOptions();

            using var handle = Proxy.git_submodule_lookup(repo.Handle, name) ?? throw new NotFoundException("Submodule lookup failed for '{0}'.", name);
            using var checkoutOptionsWrapper = new GitCheckoutOptsWrapper(options);
            using var fetchOptionsWrapper = new GitFetchOptionsWrapper();

            var gitCheckoutOptions = checkoutOptionsWrapper.Options;

            var gitFetchOptions = fetchOptionsWrapper.Options;
            gitFetchOptions.ProxyOptions = options.FetchOptions.ProxyOptions.CreateGitProxyOptions();
            gitFetchOptions.RemoteCallbacks = new RemoteCallbacks(options.FetchOptions).GenerateCallbacks();

            if (options.FetchOptions != null && options.FetchOptions.CustomHeaders != null)
            {
                gitFetchOptions.CustomHeaders =
                    GitStrArrayManaged.BuildFrom(options.FetchOptions.CustomHeaders);
            }

            var gitSubmoduleUpdateOpts = new GitSubmoduleUpdateOptions
            {
                Version = 1,
                CheckoutOptions = gitCheckoutOptions,
                FetchOptions = gitFetchOptions
            };

            Proxy.git_submodule_update(handle, options.Init, ref gitSubmoduleUpdateOpts);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Submodule> GetEnumerator()
        {
            return Proxy.git_submodule_foreach(repo.Handle, (h, n) => LaxUtf8Marshaler.FromNative(n))
                        .Select(n => this[n])
                        .GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal bool TryStage(string relativePath, bool writeIndex)
        {
            return Lookup(relativePath,
                          handle =>
                          {
                              if (handle == null)
                                  return false;

                              Proxy.git_submodule_add_to_index(handle, writeIndex);
                              return true;
                          });
        }

        internal T Lookup<T>(string name, Func<SubmoduleHandle, T> selector, bool throwIfNotFound = false)
        {
            using (var handle = Proxy.git_submodule_lookup(repo.Handle, name))
            {
                if (handle != null)
                {
                    Proxy.git_submodule_reload(handle);
                    return selector(handle);
                }

                if (throwIfNotFound)
                {
                    throw new LibGit2SharpException("Submodule lookup failed for '{0}'.", name);
                }

                return default(T);
            }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "Count = {0}", this.Count());
            }
        }
    }
}
