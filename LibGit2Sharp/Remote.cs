using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A remote repository whose branches are tracked.
    /// </summary>
    public class Remote : IEquatable<Remote>
    {
        private static readonly LambdaEqualityHelper<Remote> equalityHelper =
            new LambdaEqualityHelper<Remote>(new Func<Remote, object>[] { x => x.Name, x => x.Url });

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Remote()
        { }

        internal static Remote BuildFromPtr(RemoteSafeHandle handle)
        {
            string name = Proxy.git_remote_name(handle);
            string url = Proxy.git_remote_url(handle);

            var remote = new Remote
                             {
                                 Name = name,
                                 Url = url,
                             };

            return remote;
        }

        /// <summary>
        ///   Gets the alias of this remote repository.
        /// </summary>
        public virtual string Name { get; private set; }

        /// <summary>
        ///   Gets the url to use to communicate with this remote repository.
        /// </summary>
        public virtual string Url { get; private set; }

        /// <summary>
        ///   Determines whether the specified <see cref = "Object" /> is equal to the current <see cref = "Remote" />.
        /// </summary>
        /// <param name = "obj">The <see cref = "Object" /> to compare with the current <see cref = "Remote" />.</param>
        /// <returns>True if the specified <see cref = "Object" /> is equal to the current <see cref = "Remote" />; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Remote);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "Remote" /> is equal to the current <see cref = "Remote" />.
        /// </summary>
        /// <param name = "other">The <see cref = "Remote" /> to compare with the current <see cref = "Remote" />.</param>
        /// <returns>True if the specified <see cref = "Remote" /> is equal to the current <see cref = "Remote" />; otherwise, false.</returns>
        public bool Equals(Remote other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        ///   Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        ///   Tests if two <see cref = "Remote" /> are equal.
        /// </summary>
        /// <param name = "left">First <see cref = "Remote" /> to compare.</param>
        /// <param name = "right">Second <see cref = "Remote" /> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Remote left, Remote right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Tests if two <see cref = "Remote" /> are different.
        /// </summary>
        /// <param name = "left">First <see cref = "Remote" /> to compare.</param>
        /// <param name = "right">Second <see cref = "Remote" /> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Remote left, Remote right)
        {
            return !Equals(left, right);
        }
    }
}
