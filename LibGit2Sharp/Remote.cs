using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A remote repository whose branches are tracked.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Remote : IBelongToARepository, IDisposable
    {
        internal readonly Repository repository;

        private readonly RefSpecCollection refSpecs;

        readonly RemoteHandle handle;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Remote()
        { }

        internal Remote(RemoteHandle handle, Repository repository)
        {
            this.repository = repository;
            this.handle = handle;
            refSpecs = new RefSpecCollection(this, handle);
            repository.RegisterForCleanup(this);
        }

        /// <summary>
        /// The finalizer for the <see cref="Remote"/> class.
        /// </summary>
        ~Remote()
        {
            Dispose(false);
        }

        #region IDisposable

        bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Release the unmanaged remote object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (handle != null)
                {
                    handle.Dispose();
                }

                disposedValue = true;
            }
        }

        #endregion

        /// <summary>
        /// Gets the alias of this remote repository.
        /// </summary>
        public virtual string Name
        {
            get { return Proxy.git_remote_name(handle); }
        }

        /// <summary>
        /// Gets the url to use to communicate with this remote repository.
        /// </summary>
        public virtual string Url
        {
            get { return Proxy.git_remote_url(handle); }
        }

        /// <summary>
        /// Gets the distinct push url for this remote repository, if set.
        /// Defaults to the fetch url (<see cref="Url"/>) if not set.
        /// </summary>
        public virtual string PushUrl
        {
            get { return Proxy.git_remote_pushurl(handle) ?? Url; }
        }

        /// <summary>
        /// Gets the Tag Fetch Mode of the remote - indicating how tags are fetched.
        /// </summary>
        public virtual TagFetchMode TagFetchMode
        {
            get { return Proxy.git_remote_autotag(handle); }
        }

        /// <summary>
        /// Gets the list of <see cref="RefSpec"/>s defined for this <see cref="Remote"/>
        /// </summary>
        public virtual IEnumerable<RefSpec> RefSpecs { get { return refSpecs; } }

        /// <summary>
        /// Gets the list of <see cref="RefSpec"/>s defined for this <see cref="Remote"/>
        /// that are intended to be used during a Fetch operation
        /// </summary>
        public virtual IEnumerable<RefSpec> FetchRefSpecs
        {
            get { return refSpecs.Where(r => r.Direction == RefSpecDirection.Fetch); }
        }

        /// <summary>
        /// Gets the list of <see cref="RefSpec"/>s defined for this <see cref="Remote"/>
        /// that are intended to be used during a Push operation
        /// </summary>
        public virtual IEnumerable<RefSpec> PushRefSpecs
        {
            get { return refSpecs.Where(r => r.Direction == RefSpecDirection.Push); }
        }

        /// <summary>
        /// Transform a reference to its source reference using the <see cref="Remote"/>'s default fetchspec.
        /// </summary>
        /// <param name="reference">The reference to transform.</param>
        /// <returns>The transformed reference.</returns>
        internal unsafe string FetchSpecTransformToSource(string reference)
        {
            using (RemoteHandle remoteHandle = Proxy.git_remote_lookup(repository.Handle, Name, true))
            {
                git_refspec* fetchSpecPtr = Proxy.git_remote_get_refspec(remoteHandle, 0);
                return Proxy.git_refspec_rtransform(new IntPtr(fetchSpecPtr), reference);
            }
        }

        /// <summary>
        /// Determines if the proposed remote name is well-formed.
        /// </summary>
        /// <param name="name">The name to be checked.</param>
        /// <returns>true if the name is valid; false otherwise.</returns>
        public static bool IsValidName(string name)
        {
            return Proxy.git_remote_is_valid_name(name);
        }

        /// <summary>
        /// Gets the configured behavior regarding the deletion
        /// of stale remote tracking branches.
        /// <para>
        ///   If defined, will return the value of the <code>remote.&lt;name&gt;.prune</code> entry.
        ///   Otherwise return the value of <code>fetch.prune</code>.
        /// </para>
        /// </summary>
        public virtual bool AutomaticallyPruneOnFetch
        {
            get
            {
                var remotePrune = repository.Config.Get<bool>("remote", Name, "prune");

                if (remotePrune != null)
                {
                    return remotePrune.Value;
                }

                var fetchPrune = repository.Config.Get<bool>("fetch.prune");

                return fetchPrune != null && fetchPrune.Value;
            }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} => {1}", Name, Url);
            }
        }

        IRepository IBelongToARepository.Repository { get { return repository; } }
    }
}
