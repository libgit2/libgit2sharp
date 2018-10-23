using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// The collection of worktrees in a <see cref="Repository"/>
    /// </summary>
    public class WorktreeCollection : IEnumerable<Worktree>
    {
        internal readonly Repository repo;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected WorktreeCollection()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2Sharp.WorktreeCollection"/> class.
        /// </summary>
        /// <param name="repo">The repo.</param>
        internal WorktreeCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        /// Gets the <see cref="LibGit2Sharp.Submodule"/> with the specified name.
        /// </summary>
        public virtual Worktree this[string name]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(name, "name");

                return Lookup(name, handle => new Worktree(repo,
                    name,
                    new Repository(handle),
                    Proxy.git_worktree_is_locked(handle)));
            }
        }

        internal T Lookup<T>(string name, Func<WorktreeHandle, T> selector, bool throwIfNotFound = false)
        {
            using (var handle = Proxy.git_worktree_lookup(repo.Handle, name))
            {
                if (handle != null && Proxy.git_worktree_validate(handle))
                {
                    return selector(handle);
                }

                if (throwIfNotFound)
                {
                    throw new LibGit2SharpException("Worktree lookup failed for '{0}'.", name);
                }

                return default(T);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Worktree> GetEnumerator()
        {
            return Proxy.git_worktree_list(repo.Handle)
                .Select(n => Lookup(n, handle => new Worktree(repo, n,
                    new Repository(handle), Proxy.git_worktree_is_locked(handle))))
                .GetEnumerator();
            //return Proxy.git_submodule_foreach(repo.Handle, (h, n) => LaxUtf8Marshaler.FromNative(n))
            //            .Select(n => this[n])
            //            .GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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
