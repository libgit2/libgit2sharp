using System;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A remote repository whose branches are tracked.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Remote : IEquatable<Remote>
    {
        private static readonly LambdaEqualityHelper<Remote> equalityHelper =
            new LambdaEqualityHelper<Remote>(x => x.Name, x => x.Url);

        private readonly Repository repository;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Remote()
        { }

        private Remote(Repository repository, string name, string url, TagFetchMode tagFetchMode)
        {
            this.repository = repository;
            Name = name;
            Url = url;
            TagFetchMode = tagFetchMode;
        }

        internal static Remote BuildFromPtr(RemoteSafeHandle handle, Repository repo)
        {
            string name = Proxy.git_remote_name(handle);
            string url = Proxy.git_remote_url(handle);
            TagFetchMode tagFetchMode = Proxy.git_remote_autotag(handle);

            var remote = new Remote(repo, name, url, tagFetchMode);

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
        /// Gets the Tag Fetch Mode of the remote - indicating how tags are fetched.
        /// </summary>
        public virtual TagFetchMode TagFetchMode { get; private set; }

        /// <summary>
        /// Transform a reference to its source reference using the <see cref="Remote"/>'s default fetchspec.
        /// </summary>
        /// <param name="reference">The reference to transform.</param>
        /// <returns>The transformed reference.</returns>
        internal string FetchSpecTransformToSource(string reference)
        {
            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_load(repository.Handle, Name, true))
            {
                GitRefSpecHandle fetchSpecPtr = Proxy.git_remote_get_refspec(remoteHandle, 0);
                return Proxy.git_refspec_rtransform(fetchSpecPtr, reference);
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
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} => {1}", Name, Url);
            }
        }
    }
}
