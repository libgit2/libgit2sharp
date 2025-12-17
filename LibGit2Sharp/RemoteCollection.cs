using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// The collection of <see cref="Remote"/> in a <see cref="Repository"/>
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class RemoteCollection : IEnumerable<Remote>
    {
        private readonly Repository repository;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected RemoteCollection()
        { }

        internal RemoteCollection(Repository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// Gets the <see cref="Remote"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of the remote to retrieve.</param>
        /// <returns>The retrived <see cref="Remote"/> if it has been found, null otherwise.</returns>
        public virtual Remote this[string name]
        {
            get { return RemoteForName(name, false); }
        }

        internal Remote RemoteForName(string name, bool shouldThrowIfNotFound = true)
        {
            Ensure.ArgumentNotNull(name, "name");

            RemoteHandle handle = Proxy.git_remote_lookup(repository.Handle, name, shouldThrowIfNotFound);
            return handle == null ? null : new Remote(handle, this.repository);
        }

        /// <summary>
        /// Update properties of a remote.
        ///
        /// These updates will be performed as a bulk update at the end of the method.
        /// </summary>
        /// <param name="remote">The name of the remote to update.</param>
        /// <param name="actions">Delegate to perform updates on the remote.</param>
        public virtual void Update(string remote, params Action<RemoteUpdater>[] actions)
        {
            var updater = new RemoteUpdater(repository, remote);

            repository.Config.WithinTransaction(() =>
            {
                foreach (Action<RemoteUpdater> action in actions)
                {
                    action(updater);
                }
            });
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Remote> GetEnumerator()
        {
            return Proxy
                .git_remote_list(repository.Handle)
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

        /// <summary>
        /// Creates a <see cref="Remote"/> with the specified name and for the repository at the specified location.
        /// <para>
        ///   A default fetch refspec will be added for this remote.
        /// </para>
        /// </summary>
        /// <param name="name">The name of the remote to create.</param>
        /// <param name="url">The location of the repository.</param>
        /// <returns>A new <see cref="Remote"/>.</returns>
        public virtual Remote Add(string name, string url)
        {
            Ensure.ArgumentNotNull(name, "name");
            Ensure.ArgumentNotNull(url, "url");

            RemoteHandle handle = Proxy.git_remote_create(repository.Handle, name, url);
            return new Remote(handle, this.repository);
        }

        /// <summary>
        /// Creates a <see cref="Remote"/> with the specified name and for the repository at the specified location.
        /// </summary>
        /// <param name="name">The name of the remote to create.</param>
        /// <param name="url">The location of the repository.</param>
        /// <param name="fetchRefSpec">The refSpec to be used when fetching from this remote.</param>
        /// <returns>A new <see cref="Remote"/>.</returns>
        public virtual Remote Add(string name, string url, string fetchRefSpec)
        {
            Ensure.ArgumentNotNull(name, "name");
            Ensure.ArgumentNotNull(url, "url");
            Ensure.ArgumentNotNull(fetchRefSpec, "fetchRefSpec");

            RemoteHandle handle = Proxy.git_remote_create_with_fetchspec(repository.Handle, name, url, fetchRefSpec);
            return new Remote(handle, this.repository);
        }

        /// <summary>
        /// Deletes the <see cref="Remote"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of the remote to remove.</param>
        /// <returns>A new <see cref="Remote"/>.</returns>
        public virtual void Remove(string name)
        {
            Ensure.ArgumentNotNull(name, "name");

            Proxy.git_remote_delete(repository.Handle, name);
        }

        /// <summary>
        /// Renames an existing <see cref="Remote"/>.
        /// </summary>
        /// <param name="name">The current remote name.</param>
        /// <param name="newName">The new name the existing remote should bear.</param>
        /// <returns>A new <see cref="Remote"/>.</returns>
        public virtual Remote Rename(string name, string newName)
        {
            return Rename(name, newName, null);
        }

        /// <summary>
        /// Renames an existing <see cref="Remote"/>.
        /// </summary>
        /// <param name="name">The current remote name.</param>
        /// <param name="newName">The new name the existing remote should bear.</param>
        /// <param name="callback">The callback to be used when problems with renaming occur. (e.g. non-default fetch refspecs)</param>
        /// <returns>A new <see cref="Remote"/>.</returns>
        public virtual Remote Rename(string name, string newName, RemoteRenameFailureHandler callback)
        {
            Ensure.ArgumentNotNull(name, "name");
            Ensure.ArgumentNotNull(newName, "newName");

            Proxy.git_remote_rename(repository.Handle, name, newName, callback);
            return this[newName];
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
