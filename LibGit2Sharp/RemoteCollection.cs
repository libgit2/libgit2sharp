using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The collection of <see cref = "Remote" /> in a <see cref = "Repository" />
    /// </summary>
    public class RemoteCollection : IEnumerable<Remote>
    {
        private readonly Repository repository;

        internal RemoteCollection(Repository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        ///   Gets the <see cref = "Remote" /> with the specified name.
        /// </summary>
        /// <param name = "name">The name of the remote to retrieve.</param>
        /// <returns>The retrived <see cref = "Remote" /> if it has been found, null otherwise.</returns>
        public Remote this[string name]
        {
            get { return RemoteForName(name); }
        }

        internal RemoteSafeHandle LoadRemote(string name, bool throwsIfNotFound)
        {
            RemoteSafeHandle handle;

            int res = NativeMethods.git_remote_load(out handle, repository.Handle, name);

            if (res == (int)GitErrorCode.GIT_ENOTFOUND && !throwsIfNotFound)
            {
                return null;
            }

            Ensure.Success(res);

            return handle;
        }

        private Remote RemoteForName(string name)
        {
            using (RemoteSafeHandle handle = LoadRemote(name, false))
            {
                return Remote.CreateFromPtr(handle);
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Remote> GetEnumerator()
        {
            return Libgit2UnsafeHelper
                .ListAllRemoteNames(repository.Handle)
                .Select(n => this[n])
                .GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///   Creates a <see cref="Remote"/> with the specified name and for the repository at the specified location
        /// </summary>
        /// <param name = "name">The name of the remote to create.</param>
        /// <param name = "url">The location of the repository.</param>
        /// <returns>A new <see cref = "Remote" />.</returns>
        public Remote Create(string name, string url)
        {
            Ensure.ArgumentNotNull(name, "name");
            Ensure.ArgumentNotNull(url, "url");

            RemoteSafeHandle handle;
            int res = NativeMethods.git_remote_new(out handle, repository.Handle, url, name);
            Ensure.Success(res);

            using (handle)
            {
                res = NativeMethods.git_remote_save(handle);
                Ensure.Success(res);

                return Remote.CreateFromPtr(handle);
            }
        }
    }
}
