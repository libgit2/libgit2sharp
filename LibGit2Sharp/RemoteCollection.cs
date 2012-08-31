using System;
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

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected RemoteCollection()
        { }

        internal RemoteCollection(Repository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        ///   Gets the <see cref = "Remote" /> with the specified name.
        /// </summary>
        /// <param name = "name">The name of the remote to retrieve.</param>
        /// <returns>The retrived <see cref = "Remote" /> if it has been found, null otherwise.</returns>
        public virtual Remote this[string name]
        {
            get { return RemoteForName(name); }
        }

        private Remote RemoteForName(string name)
        {
            using (RemoteSafeHandle handle = Proxy.git_remote_load(repository.Handle, name, false))
            {
                return handle == null ? null : Remote.BuildFromPtr(handle);
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Remote> GetEnumerator()
        {
            return Proxy
                .git_remote_list(repository.Handle)
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
        ///   <para>
        ///     A default fetch refspec will be added for this remote.
        ///   </para>
        /// </summary>
        /// <param name = "name">The name of the remote to create.</param>
        /// <param name = "url">The location of the repository.</param>
        /// <returns>A new <see cref = "Remote" />.</returns>
        public virtual Remote Add(string name, string url)
        {
            Ensure.ArgumentNotNull(name, "name");
            Ensure.ArgumentNotNull(url, "url");

            using (RemoteSafeHandle handle = Proxy.git_remote_add(repository.Handle, name, url))
            {
                return Remote.BuildFromPtr(handle);
            }
        }

        /// <summary>
        ///   Creates a <see cref="Remote"/> with the specified name and for the repository at the specified location.
        ///   <para>
        ///     A default fetch refspec will be added for this remote.
        ///   </para>
        /// </summary>
        /// <param name = "name">The name of the remote to create.</param>
        /// <param name = "url">The location of the repository.</param>
        /// <returns>A new <see cref = "Remote" />.</returns>
        [Obsolete("This method will be removed in the next release. Please use Add() instead.")]
        public virtual Remote Create(string name, string url)
        {
            return Add(name, url);
        }

        /// <summary>
        ///   Creates a <see cref="Remote"/> with the specified name and for the repository at the specified location.
        /// </summary>
        /// <param name = "name">The name of the remote to create.</param>
        /// <param name = "url">The location of the repository.</param>
        /// <param name = "fetchRefSpec">The refSpec to be used when fetching from this remote..</param>
        /// <returns>A new <see cref = "Remote" />.</returns>
        public virtual Remote Add(string name, string url, string fetchRefSpec)
        {
            Ensure.ArgumentNotNull(name, "name");
            Ensure.ArgumentNotNull(url, "url");
            Ensure.ArgumentNotNull(fetchRefSpec, "fetchRefSpec");

            using (RemoteSafeHandle handle = Proxy.git_remote_new(repository.Handle, name, url, fetchRefSpec))
            {
                Proxy.git_remote_save(handle);
                return Remote.BuildFromPtr(handle);
            }
        }

        /// <summary>
        ///   Creates a <see cref="Remote"/> with the specified name and for the repository at the specified location.
        /// </summary>
        /// <param name = "name">The name of the remote to create.</param>
        /// <param name = "url">The location of the repository.</param>
        /// <param name = "fetchRefSpec">The refSpec to be used when fetching from this remote..</param>
        /// <returns>A new <see cref = "Remote" />.</returns>
        [Obsolete("This method will be removed in the next release. Please use Add() instead.")]
        public virtual Remote Create(string name, string url, string fetchRefSpec)
        {
            return Add(name, url);
        }
    }
}
