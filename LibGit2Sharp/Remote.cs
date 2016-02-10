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
    public class Remote : IEquatable<Remote>, IBelongToARepository
    {
        private static readonly LambdaEqualityHelper<Remote> equalityHelper =
            new LambdaEqualityHelper<Remote>(x => x.Name, x => x.Url, x => x.PushUrl);

        internal readonly Repository repository;

        private readonly RefSpecCollection refSpecs;
        private string pushUrl;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Remote()
        { }

        private Remote(RemoteSafeHandle handle, Repository repository)
        {
            this.repository = repository;
            Name = Proxy.git_remote_name(handle);
            Url = Proxy.git_remote_url(handle);
            PushUrl = Proxy.git_remote_pushurl(handle);
            TagFetchMode = Proxy.git_remote_autotag(handle);
            refSpecs = new RefSpecCollection(handle);
        }

        internal static Remote BuildFromPtr(RemoteSafeHandle handle, Repository repo)
        {
            var remote = new Remote(handle, repo);

            return remote;
        }

        /// <summary>
        /// Gets the alias of this remote repository.
        /// </summary>
        public virtual string Name { get; private set; }

        /// <summary>
        /// Gets the url to use to communicate with this remote repository.
        /// </summary>
        public virtual string Url { get; private set; }

        /// <summary>
        /// Gets the distinct push url for this remote repository, if set.
        /// Defaults to the fetch url (<see cref="Url"/>) if not set.
        /// </summary>
        public virtual string PushUrl
        {
            get { return pushUrl ?? Url; }
            private set { pushUrl = value; }
        }

        /// <summary>
        /// Gets the Tag Fetch Mode of the remote - indicating how tags are fetched.
        /// </summary>
        public virtual TagFetchMode TagFetchMode { get; private set; }

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
        internal string FetchSpecTransformToSource(string reference)
        {
            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_lookup(repository.Handle, Name, true))
            {
                GitRefSpecHandle fetchSpecPtr = Proxy.git_remote_get_refspec(remoteHandle, 0);
                return Proxy.git_refspec_rtransform(fetchSpecPtr, reference);
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

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Remote"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Remote"/>.</param>
        /// <returns>True if the specified <see cref="Object"/> is equal to the current <see cref="Remote"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Remote);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Remote"/> is equal to the current <see cref="Remote"/>.
        /// </summary>
        /// <param name="other">The <see cref="Remote"/> to compare with the current <see cref="Remote"/>.</param>
        /// <returns>True if the specified <see cref="Remote"/> is equal to the current <see cref="Remote"/>; otherwise, false.</returns>
        public bool Equals(Remote other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        /// Tests if two <see cref="Remote"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="Remote"/> to compare.</param>
        /// <param name="right">Second <see cref="Remote"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Remote left, Remote right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="Remote"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="Remote"/> to compare.</param>
        /// <param name="right">Second <see cref="Remote"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Remote left, Remote right)
        {
            return !Equals(left, right);
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
