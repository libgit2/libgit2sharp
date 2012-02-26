using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;

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
    }
}
