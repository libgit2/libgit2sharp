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

        private Repository repository;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Remote()
        { }


        internal static Remote CreateFromPtr(Repository repository, RemoteSafeHandle handle)
        {
            if (handle == null)
            {
                return null;
            }

            string name = NativeMethods.git_remote_name(handle);
            string url = NativeMethods.git_remote_url(handle);

            var remote = new Remote
                             {
                                 Name = name,
                                 Url = url,
                                 repository = repository,
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
        ///   Fetch updates from the remote.
        /// </summary>
        /// <param name="fetchProgress">The <see cref = "FetchProgress" /> to report current fetch progress.</param>
        public virtual void Fetch(FetchProgress fetchProgress)
        {
            // reset the current progress object
            fetchProgress.Reset();

            using (RemoteSafeHandle remoteHandle = repository.Remotes.LoadRemote(Name, true))
            {
                try
                {
                    int res = NativeMethods.git_remote_connect(remoteHandle, GitDirection.Fetch);
                    Ensure.Success(res);

                    int downloadResult = NativeMethods.git_remote_download(remoteHandle, ref fetchProgress.bytes, ref fetchProgress.indexerStats);
                    Ensure.Success(downloadResult);
                }
                finally
                {
                    if (remoteHandle != null)
                    {
                        NativeMethods.git_remote_disconnect(remoteHandle);
                    }
                }

                // update references
                int updateTipsResult = NativeMethods.git_remote_update_tips(remoteHandle, IntPtr.Zero);
                Ensure.Success(updateTipsResult);
            }
        }

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
