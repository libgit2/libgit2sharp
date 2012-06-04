﻿using System.Collections;
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
        ///   Creates a <see cref="Remote"/> with the specified name and for the repository at the specified location.
        /// </summary>
        /// <param name = "name">The name of the remote to create.</param>
        /// <param name = "url">The location of the repository.</param>
        /// <param name = "fetchRefSpec">The refSpec to be used when fetching from this remote..</param>
        /// <returns>A new <see cref = "Remote" />.</returns>
        public Remote Add(string name, string url, string fetchRefSpec = null)
        {
            Ensure.ArgumentNotNull(name, "name");
            Ensure.ArgumentNotNull(url, "url");

            RemoteSafeHandle handle;

            if(fetchRefSpec == null)
            {
                // TODO: libgit2 doesn't use the correct default refspec right now.
                // This PR fixes it: https://github.com/libgit2/libgit2/pull/737
                // We can then get rid of having these kinds of defaults in the C# code
                //Ensure.Success(NativeMethods.git_remote_add(out handle, repository.Handle, name, url));

                fetchRefSpec = string.Format("+refs/heads/*:refs/remotes/{0}/*", name);
                Ensure.Success(NativeMethods.git_remote_new(out handle, repository.Handle, name, url, fetchRefSpec));
            }
            else
            {
                Ensure.Success(NativeMethods.git_remote_new(out handle, repository.Handle, name, url, fetchRefSpec));
            }

            using (handle)
            {
                Ensure.Success(NativeMethods.git_remote_save(handle));

                return Remote.CreateFromPtr(handle);
            }
        }
    }
}
