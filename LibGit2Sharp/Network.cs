using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides access to network functionality for a repository.
    /// </summary>
    public class Network
    {
        private readonly Repository repository;
        private readonly Lazy<RemoteCollection> remotes;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Network()
        { }

        internal Network(Repository repository)
        {
            this.repository = repository;
            remotes = new Lazy<RemoteCollection>(() => new RemoteCollection(repository));
        }

        /// <summary>
        /// Lookup and manage remotes in the repository.
        /// </summary>
        public virtual RemoteCollection Remotes
        {
            get { return remotes.Value; }
        }

        /// <summary>
        /// List references in a <see cref="Remote"/> repository.
        /// <para>
        /// When the remote tips are ahead of the local ones, the retrieved
        /// <see cref="DirectReference"/>s may point to non existing
        /// <see cref="GitObject"/>s in the local repository. In that
        /// case, <see cref="DirectReference.Target"/> will return <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to list from.</param>
        /// <param name="credentialsProvider">The optional <see cref="Func{Credentials}"/> used to connect to remote repository.</param>
        /// <returns>The references in the <see cref="Remote"/> repository.</returns>
        public virtual IEnumerable<DirectReference> ListReferences(Remote remote, CredentialsHandler credentialsProvider = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");

            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_load(repository.Handle, remote.Name, true))
            {
                if (credentialsProvider != null)
                {
                    var callbacks = new RemoteCallbacks(credentialsProvider);
                    GitRemoteCallbacks gitCallbacks = callbacks.GenerateCallbacks();
                    Proxy.git_remote_set_callbacks(remoteHandle, ref gitCallbacks);
                }

                Proxy.git_remote_connect(remoteHandle, GitDirection.Fetch);
                return Proxy.git_remote_ls(repository, remoteHandle);
            }
        }

        /// <summary>
        /// List references in a remote repository.
        /// <para>
        /// When the remote tips are ahead of the local ones, the retrieved
        /// <see cref="DirectReference"/>s may point to non existing
        /// <see cref="GitObject"/>s in the local repository. In that
        /// case, <see cref="DirectReference.Target"/> will return <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="url">The url to list from.</param>
        /// <returns>The references in the remote repository.</returns>
        public virtual IEnumerable<DirectReference> ListReferences(string url)
        {
            Ensure.ArgumentNotNull(url, "url");

            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_create_anonymous(repository.Handle, url, null))
            {
                Proxy.git_remote_connect(remoteHandle, GitDirection.Fetch);
                return Proxy.git_remote_ls(repository, remoteHandle);
            }
        }

        static void DoFetch(RemoteSafeHandle remoteHandle, FetchOptions options, Signature signature, string logMessage)
        {
            if (options == null)
            {
                options = new FetchOptions();
            }

            if (options.TagFetchMode.HasValue)
            {
                Proxy.git_remote_set_autotag(remoteHandle, options.TagFetchMode.Value);
            }

            var callbacks = new RemoteCallbacks(options);
            GitRemoteCallbacks gitCallbacks = callbacks.GenerateCallbacks();

            // It is OK to pass the reference to the GitCallbacks directly here because libgit2 makes a copy of
            // the data in the git_remote_callbacks structure. If, in the future, libgit2 changes its implementation
            // to store a reference to the git_remote_callbacks structure this would introduce a subtle bug
            // where the managed layer could move the git_remote_callbacks to a different location in memory,
            // but libgit2 would still reference the old address.
            //
            // Also, if GitRemoteCallbacks were a class instead of a struct, we would need to guard against
            // GC occuring in between setting the remote callbacks and actual usage in one of the functions afterwords.
            Proxy.git_remote_set_callbacks(remoteHandle, ref gitCallbacks);

            Proxy.git_remote_fetch(remoteHandle, signature, logMessage);
        }

        /// <summary>
        /// Fetch from the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The remote to fetch</param>
        /// <param name="options"><see cref="FetchOptions"/> controlling fetch behavior</param>
        /// <param name="signature">Identity for use when updating the reflog.</param>
        /// <param name="logMessage">Message to use when updating the reflog.</param>
        public virtual void Fetch(Remote remote, FetchOptions options = null,
            Signature signature = null,
            string logMessage = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");

            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_load(repository.Handle, remote.Name, true))
            {
                DoFetch(remoteHandle, options, signature.OrDefault(repository.Config), logMessage);
            }
        }

        /// <summary>
        /// Fetch from the <see cref="Remote"/>, using custom refspecs.
        /// </summary>
        /// <param name="remote">The remote to fetch</param>
        /// <param name="refspecs">Refspecs to use, replacing the remote's fetch refspecs</param>
        /// <param name="options"><see cref="FetchOptions"/> controlling fetch behavior</param>
        /// <param name="signature">Identity for use when updating the reflog.</param>
        /// <param name="logMessage">Message to use when updating the reflog.</param>
        public virtual void Fetch(Remote remote, IEnumerable<string> refspecs, FetchOptions options = null,
            Signature signature = null,
            string logMessage = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(refspecs, "refspecs");

            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_load(repository.Handle, remote.Name, true))
            {
                Proxy.git_remote_set_fetch_refspecs(remoteHandle, refspecs);

                DoFetch(remoteHandle, options, signature.OrDefault(repository.Config), logMessage);
            }
        }

        /// <summary>
        /// Fetch from a url with a set of fetch refspecs
        /// </summary>
        /// <param name="url">The url to fetch from</param>
        /// <param name="refspecs">The list of resfpecs to use</param>
        /// <param name="options"><see cref="FetchOptions"/> controlling fetch behavior</param>
        /// <param name="signature">Identity for use when updating the reflog.</param>
        /// <param name="logMessage">Message to use when updating the reflog.</param>
        public virtual void Fetch(
            string url,
            IEnumerable<string> refspecs,
            FetchOptions options = null,
            Signature signature = null,
            string logMessage = null)
        {
            Ensure.ArgumentNotNull(url, "url");
            Ensure.ArgumentNotNull(refspecs, "refspecs");

            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_create_anonymous(repository.Handle, url, null))
            {
                Proxy.git_remote_set_fetch_refspecs(remoteHandle, refspecs);

                DoFetch(remoteHandle, options, signature.OrDefault(repository.Config), logMessage);
            }
        }

        /// <summary>
        /// Push the objectish to the destination reference on the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="objectish">The source objectish to push.</param>
        /// <param name="destinationSpec">The reference to update on the remote.</param>
        /// <param name="pushOptions"><see cref="PushOptions"/> controlling push behavior</param>
        /// <param name="signature">Identity for use when updating the reflog.</param>
        /// <param name="logMessage">Message to use when updating the reflog.</param>
        public virtual void Push(
            Remote remote,
            string objectish,
            string destinationSpec,
            PushOptions pushOptions = null,
            Signature signature = null,
            string logMessage = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(objectish, "objectish");
            Ensure.ArgumentNotNullOrEmptyString(destinationSpec, destinationSpec);

            Push(remote, string.Format(CultureInfo.InvariantCulture,
                "{0}:{1}", objectish, destinationSpec), pushOptions, signature, logMessage);
        }

        /// <summary>
        /// Push specified reference to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="pushRefSpec">The pushRefSpec to push.</param>
        /// <param name="pushOptions"><see cref="PushOptions"/> controlling push behavior</param>
        /// <param name="signature">Identity for use when updating the reflog.</param>
        /// <param name="logMessage">Message to use when updating the reflog.</param>
        public virtual void Push(
            Remote remote,
            string pushRefSpec,
            PushOptions pushOptions = null,
            Signature signature = null,
            string logMessage = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNullOrEmptyString(pushRefSpec, "pushRefSpec");

            Push(remote, new[] { pushRefSpec }, pushOptions, signature, logMessage);
        }

        /// <summary>
        /// Push specified references to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="pushRefSpecs">The pushRefSpecs to push.</param>
        /// <param name="pushOptions"><see cref="PushOptions"/> controlling push behavior</param>
        /// <param name="signature">Identity for use when updating the reflog.</param>
        /// <param name="logMessage">Message to use when updating the reflog.</param>
        public virtual void Push(
            Remote remote,
            IEnumerable<string> pushRefSpecs,
            PushOptions pushOptions = null,
            Signature signature = null,
            string logMessage = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(pushRefSpecs, "pushRefSpecs");

            // The following local variables are protected from garbage collection
            // by a GC.KeepAlive call at the end of the method. Otherwise,
            // random crashes during push progress reporting could occur.
            PushTransferCallbacks pushTransferCallbacks;
            PackbuilderCallbacks packBuilderCallbacks;
            NativeMethods.git_push_transfer_progress pushProgress;
            NativeMethods.git_packbuilder_progress packBuilderProgress;

            // Return early if there is nothing to push.
            if (!pushRefSpecs.Any())
            {
                return;
            }

            if (pushOptions == null)
            {
                pushOptions = new PushOptions();
            }

            PushCallbacks pushStatusUpdates = new PushCallbacks(pushOptions.OnPushStatusError);

            // Load the remote.
            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_load(repository.Handle, remote.Name, true))
            {
                var callbacks = new RemoteCallbacks(pushOptions.CredentialsProvider);
                GitRemoteCallbacks gitCallbacks = callbacks.GenerateCallbacks();
                Proxy.git_remote_set_callbacks(remoteHandle, ref gitCallbacks);

                try
                {
                    Proxy.git_remote_connect(remoteHandle, GitDirection.Push);

                    // Perform the actual push.
                    using (PushSafeHandle pushHandle = Proxy.git_push_new(remoteHandle))
                    {
                        pushTransferCallbacks = new PushTransferCallbacks(pushOptions.OnPushTransferProgress);
                        packBuilderCallbacks = new PackbuilderCallbacks(pushOptions.OnPackBuilderProgress);

                        pushProgress = pushTransferCallbacks.GenerateCallback();
                        packBuilderProgress = packBuilderCallbacks.GenerateCallback();

                        Proxy.git_push_set_callbacks(pushHandle, pushProgress, packBuilderProgress);

                        // Set push options.
                        Proxy.git_push_set_options(pushHandle,
                            new GitPushOptions()
                            {
                                PackbuilderDegreeOfParallelism = pushOptions.PackbuilderDegreeOfParallelism
                            });

                        // Add refspecs.
                        foreach (string pushRefSpec in pushRefSpecs)
                        {
                            Proxy.git_push_add_refspec(pushHandle, pushRefSpec);
                        }

                        Proxy.git_push_finish(pushHandle);

                        if (!Proxy.git_push_unpack_ok(pushHandle))
                        {
                            throw new LibGit2SharpException("Push failed - remote did not successfully unpack.");
                        }

                        Proxy.git_push_status_foreach(pushHandle, pushStatusUpdates.Callback);
                        Proxy.git_push_update_tips(pushHandle, signature.OrDefault(repository.Config), logMessage);
                    }
                }
                finally
                {
                    Proxy.git_remote_disconnect(remoteHandle);
                }
            }

            GC.KeepAlive(pushProgress);
            GC.KeepAlive(packBuilderProgress);
            GC.KeepAlive(pushTransferCallbacks);
            GC.KeepAlive(packBuilderCallbacks);
        }

        /// <summary>
        /// Pull changes from the configured upstream remote and branch into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="merger">If the merge is a non-fast forward merge that generates a merge commit, the <see cref="Signature"/> of who made the merge.</param>
        /// <param name="options">Specifies optional parameters controlling merge behavior of pull; if null, the defaults are used.</param>
        public virtual MergeResult Pull(Signature merger, PullOptions options)
        {
            Ensure.ArgumentNotNull(merger, "merger");
            Ensure.ArgumentNotNull(options, "options");

            Branch currentBranch = repository.Head;

            if(!currentBranch.IsTracking)
            {
                throw new LibGit2SharpException("There is no tracking information for the current branch.");
            }

            if (currentBranch.Remote == null)
            {
                throw new LibGit2SharpException("No upstream remote for the current branch.");
            }

            Fetch(currentBranch.Remote, options.FetchOptions);
            return repository.MergeFetchHeads(merger, options.MergeOptions);
        }

        /// <summary>
        /// The heads that have been updated during the last fetch.
        /// </summary>
        internal virtual IEnumerable<FetchHead> FetchHeads
        {
            get
            {
                int i = 0;

                return Proxy.git_repository_fetchhead_foreach(
                    repository.Handle,
                    (name, url, oid, isMerge) => new FetchHead(repository, name, url, oid, isMerge, i++));
            }
        }

        /// <summary>
        /// Helper class to handle callbacks during push.
        /// </summary>
        private class PushCallbacks
        {
            readonly PushStatusErrorHandler onError;

            public PushCallbacks(PushStatusErrorHandler onError)
            {
                this.onError = onError;
            }

            public int Callback(IntPtr referenceNamePtr, IntPtr msgPtr, IntPtr payload)
            {
                // Exit early if there is no callback.
                if (onError == null)
                {
                    return 0;
                }

                // The reference name pointer should never be null - if it is,
                // this indicates a bug somewhere (libgit2, server, etc).
                if (referenceNamePtr == IntPtr.Zero)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Invalid, "Not expecting null for reference name in push status.");
                    return -1;
                }

                // Only report updates where there is a message - indicating
                // that there was an error.
                if (msgPtr != IntPtr.Zero)
                {
                    string referenceName = LaxUtf8Marshaler.FromNative(referenceNamePtr);
                    string msg = LaxUtf8Marshaler.FromNative(msgPtr);
                    onError(new PushStatusError(referenceName, msg));
                }

                return 0;
            }
        }
    }
}
