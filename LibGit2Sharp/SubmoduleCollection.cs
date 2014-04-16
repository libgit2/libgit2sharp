using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
        /// Adds a new repository, checkout the selected branch and add it to superproject index  
        /// </summary>
        /// <param name="name">The name of the Submodule</param>
        /// <param name="url">The url of the remote repository</param>
        /// <param name="branch">The remote branch to checkout</param>
        /// <param name="relativePath">The path of the submodule inside of the super repository, if none, name is taken.</param>
        /// <param name="use_GitLink"></param>
        /// <returns></returns>
        public Submodule Add(string name, string url, string committish = null, string relativePath = null, bool useGitLink = true)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            Ensure.ArgumentNotNullOrEmptyString(url, "url");

            relativePath = relativePath ?? name;

            using (SubmoduleSafeHandle handle = Proxy.git_submodule_add_setup(repo.Handle, url, relativePath, useGitLink))
            {
                string subPath = Path.Combine(repo.Info.WorkingDirectory, relativePath);

                Repository.Clone(url, subPath);

                if (committish != null)
                {
                    using (Repository subRepo = new Repository(subPath))
                        subRepo.Checkout(subRepo.Branches[committish]);
                }
                    
                Proxy.git_submodule_add_finalize(handle);
            }

            return this[name];
        }

        /// <summary>
        /// Gets the <see cref="LibGit2Sharp.Submodule"/> with the specified name.
        /// </summary>
        public virtual Submodule this[string name]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(name, "name");

                return Lookup(name, handle =>
                                    new Submodule(repo, name,
                                                  Proxy.git_submodule_path(handle),
                                                  Proxy.git_submodule_url(handle)));
            }
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
            return Lookup(relativePath, handle =>
                                            {
                                                if (handle == null)
                                                    return false;

                                                Proxy.git_submodule_add_to_index(handle, writeIndex);
                                                return true;
                                            });
        }

        internal T Lookup<T>(string name, Func<SubmoduleSafeHandle, T> selector, bool throwIfNotFound = false)
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
                    throw new LibGit2SharpException(string.Format(
                        CultureInfo.InvariantCulture,
                        "Submodule lookup failed for '{0}'.", name));
                }

                return default(T);
            }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "Count = {0}", this.Count());
            }
        }
    }
}
