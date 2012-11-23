using System;
using System.Diagnostics;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A remote repository whose branches are tracked.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Remote : IEquatable<Remote>
    {
        private static readonly LambdaEqualityHelper<Remote> equalityHelper =
            new LambdaEqualityHelper<Remote>(x => x.Name, x => x.Url);

        private readonly Repository repository;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Remote()
        { }

        private Remote(Repository repository, string name, string url)
        {
            this.repository = repository;
            this.Name = name;
            this.Url = url;
        }

        internal static Remote BuildFromPtr(RemoteSafeHandle handle, Repository repo)
        {
            string name = Proxy.git_remote_name(handle);
            string url = Proxy.git_remote_url(handle);

            var remote = new Remote(repo, name, url);

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
        ///   Fetch from the <see cref = "Remote" />.
        /// </summary>
        /// <param name="tagFetchMode">Optional parameter indicating what tags to download.</param>
        /// <param name="onProgress">Progress callback. Corresponds to libgit2 progress callback.</param>
        /// <param name="onCompletion">Completion callback. Corresponds to libgit2 completion callback.</param>
        /// <param name="onUpdateTips">UpdateTips callback. Corresponds to libgit2 update_tips callback.</param>
        /// <param name="onTransferProgress">Callback method that transfer progress will be reported through.
        ///   Reports the client's state regarding the received and processed (bytes, objects) from the server.</param>
        public virtual void Fetch(
            TagFetchMode tagFetchMode = TagFetchMode.Auto,
            ProgressHandler onProgress = null,
            CompletionHandler onCompletion = null,
            UpdateTipsHandler onUpdateTips = null,
            TransferProgressHandler onTransferProgress = null)
        {
            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_load(repository.Handle, this.Name, true))
            {
                var callbacks = new RemoteCallbacks(onProgress, onCompletion, onUpdateTips);
                GitRemoteCallbacks gitCallbacks = callbacks.GenerateCallbacks();

                Proxy.git_remote_set_autotag(remoteHandle, tagFetchMode);

                // It is OK to pass the reference to the GitCallbacks directly here because libgit2 makes a copy of
                // the data in the git_remote_callbacks structure. If, in the future, libgit2 changes its implementation
                // to store a reference to the git_remote_callbacks structure this would introduce a subtle bug
                // where the managed layer could move the git_remote_callbacks to a different location in memory,
                // but libgit2 would still reference the old address.
                //
                // Also, if GitRemoteCallbacks were a class instead of a struct, we would need to guard against
                // GC occuring in between setting the remote callbacks and actual usage in one of the functions afterwords.
                Proxy.git_remote_set_callbacks(remoteHandle, ref gitCallbacks); 
                
                try
                {
                    Proxy.git_remote_connect(remoteHandle, GitDirection.Fetch);
                    Proxy.git_remote_download(remoteHandle, onTransferProgress);
                }
                finally
                {
                    Proxy.git_remote_disconnect(remoteHandle);
                }

                // Update references.
                Proxy.git_remote_update_tips(remoteHandle);
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

        private string DebuggerDisplay
        {
            get { return string.Format("{0} => {1}", Name, Url); }
        }
    }
}
