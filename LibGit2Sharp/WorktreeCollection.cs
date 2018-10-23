using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
                    Proxy.git_worktree_is_locked(handle)));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="committishOrBranchSpec"></param>
        /// <param name="name"></param>
        /// <param name="path"></param>
        /// <param name="isLocked"></param>
        /// <returns></returns>
        public virtual Worktree Add(string committishOrBranchSpec, string name, string path, bool isLocked)
        {
            if(string.Equals(committishOrBranchSpec, name))
            {
                // Proxy.git_worktree_add() creates a new branch of name = name, so if we want to checkout a given branch then the 'name' cannot be the same as the target branch
                return null;
            }

            git_worktree_add_options options = new git_worktree_add_options
            {
                version = 1,
                locked = Convert.ToInt32(isLocked)
            };

            using (var handle = Proxy.git_worktree_add(repo.Handle, name, path, options))
            {
                var worktree = new Worktree(
                      repo,
                      name,
                      Proxy.git_worktree_is_locked(handle));

                // switch the worktree to the target branch
                using (var repository = worktree.WorktreeRepository)
                {
                    Commands.Checkout(repository, committishOrBranchSpec);
                }
            }

            

            return this[name]; 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        /// <param name="isLocked"></param>
        public virtual Worktree Add(string name, string path, bool isLocked)
        {
            git_worktree_add_options options = new git_worktree_add_options
            {
                version = 1,
                locked = Convert.ToInt32(isLocked)
            };

            using (var handle = Proxy.git_worktree_add(repo.Handle, name, path, options))
            {
                return new Worktree(
                   repo,
                   name,
                   Proxy.git_worktree_is_locked(handle));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="worktree"></param>
        /// <returns></returns>
        public virtual bool Prune(Worktree worktree)
        {
            return Prune(worktree, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="worktree"></param>
        /// <param name="ifLocked"></param>
        /// <returns></returns>
        public virtual bool Prune(Worktree worktree, bool ifLocked)
        {
            using (var handle = worktree.GetWorktreeHandle())
            {
                git_worktree_prune_options options = new git_worktree_prune_options
                {
                    version = 1,
                    // default
                    flags = GitWorktreePruneOptionFlags.GIT_WORKTREE_PRUNE_WORKING_TREE | GitWorktreePruneOptionFlags.GIT_WORKTREE_PRUNE_VALID
                };

                if (ifLocked)
                {
                    options.flags |= GitWorktreePruneOptionFlags.GIT_WORKTREE_PRUNE_LOCKED;
                }

                return Proxy.git_worktree_prune(handle, options);
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
                .Select(n => Lookup(n, handle => new Worktree(repo, n, Proxy.git_worktree_is_locked(handle))))
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

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "Count = {0}", this.Count());
            }
        }
    }
}
