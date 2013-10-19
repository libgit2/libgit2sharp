﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
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
        /// The heads that have been updated during the last fetch.
        /// </summary>
        public virtual IEnumerable<FetchHead> FetchHeads
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
        /// Lookup and manage remotes in the repository.
        /// </summary>
        public virtual RemoteCollection Remotes
        {
            get { return remotes.Value; }
        }

        /// <summary>
        /// List references in a <see cref="Remote"/> repository.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to list from.</param>
        /// <returns>The references in the <see cref="Remote"/> repository.</returns>
        public virtual IEnumerable<DirectReference> ListReferences(Remote remote)
        {
            Ensure.ArgumentNotNull(remote, "remote");

            List<DirectReference> directReferences = new List<DirectReference>();
            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_load(repository.Handle, remote.Name, true))
            {
                Proxy.git_remote_connect(remoteHandle, GitDirection.Fetch);

                NativeMethods.git_headlist_cb cb = (ref GitRemoteHead remoteHead, IntPtr payload) =>
                {
                    // The name pointer should never be null - if it is,
                    // this indicates a bug somewhere (libgit2, server, etc).
                    if (remoteHead.NamePtr == IntPtr.Zero)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Invalid, "Not expecting null value for reference name.");
                        return -1;
                    }

                    ObjectId oid = remoteHead.Oid;
                    string name = LaxUtf8Marshaler.FromNative(remoteHead.NamePtr);
                    directReferences.Add(new DirectReference(name, this.repository, oid));

                    return 0;
                };

                Proxy.git_remote_ls(remoteHandle, cb);
            }

            return directReferences;
        }

        /// <summary>
        /// Fetch from the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The remote to fetch</param>
        /// <param name="tagFetchMode">Optional parameter indicating what tags to download.</param>
        /// <param name="onProgress">Progress callback. Corresponds to libgit2 progress callback.</param>
        /// <param name="onUpdateTips">UpdateTips callback. Corresponds to libgit2 update_tips callback.</param>
        /// <param name="onTransferProgress">Callback method that transfer progress will be reported through.
        /// Reports the client's state regarding the received and processed (bytes, objects) from the server.</param>
        /// <param name="credentials">Credentials to use for username/password authentication.</param>
        public virtual void Fetch(
            Remote remote,
            TagFetchMode? tagFetchMode = null,
            ProgressHandler onProgress = null,
            UpdateTipsHandler onUpdateTips = null,
            TransferProgressHandler onTransferProgress = null,
            Credentials credentials = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");

            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_load(repository.Handle, remote.Name, true))
            {
                var callbacks = new RemoteCallbacks(onProgress, onTransferProgress, onUpdateTips, credentials);
                GitRemoteCallbacks gitCallbacks = callbacks.GenerateCallbacks();

                if (tagFetchMode.HasValue)
                {
                    Proxy.git_remote_set_autotag(remoteHandle, tagFetchMode.Value);
                }

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
                    Proxy.git_remote_download(remoteHandle);
                    Proxy.git_remote_update_tips(remoteHandle);
                }
                finally
                {
                    Proxy.git_remote_disconnect(remoteHandle);
                }
            }
        }

        /// <summary>
        /// Push the objectish to the destination reference on the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="objectish">The source objectish to push.</param>
        /// <param name="destinationSpec">The reference to update on the remote.</param>
        /// <param name="pushOptions"><see cref="PushOptions"/> controlling push behavior</param>
        public virtual void Push(
            Remote remote,
            string objectish,
            string destinationSpec,
            PushOptions pushOptions = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(objectish, "objectish");
            Ensure.ArgumentNotNullOrEmptyString(destinationSpec, destinationSpec);

            Push(remote, string.Format(CultureInfo.InvariantCulture,
                "{0}:{1}", objectish, destinationSpec), pushOptions);
        }

        /// <summary>
        /// Push specified reference to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="pushRefSpec">The pushRefSpec to push.</param>
        /// <param name="pushOptions"><see cref="PushOptions"/> controlling push behavior</param>
        public virtual void Push(
            Remote remote,
            string pushRefSpec,
            PushOptions pushOptions = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNullOrEmptyString(pushRefSpec, "pushRefSpec");

            Push(remote, new string[] { pushRefSpec }, pushOptions);
        }

        /// <summary>
        /// Push specified references to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="pushRefSpecs">The pushRefSpecs to push.</param>
        /// <param name="pushOptions"><see cref="PushOptions"/> controlling push behavior</param>
        public virtual void Push(
            Remote remote,
            IEnumerable<string> pushRefSpecs,
            PushOptions pushOptions = null)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(pushRefSpecs, "pushRefSpecs");

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
                var callbacks = new RemoteCallbacks(null, null, null, pushOptions.Credentials);
                GitRemoteCallbacks gitCallbacks = callbacks.GenerateCallbacks();
                Proxy.git_remote_set_callbacks(remoteHandle, ref gitCallbacks);

                try
                {
                    Proxy.git_remote_connect(remoteHandle, GitDirection.Push);

                    // Perform the actual push.
                    using (PushSafeHandle pushHandle = Proxy.git_push_new(remoteHandle))
                    {
                        PushTransferCallbacks pushTransferCallbacks = new PushTransferCallbacks(pushOptions.OnPushTransferProgress);
                        PackbuilderCallbacks packBuilderCallbacks = new PackbuilderCallbacks(pushOptions.OnPackBuilderProgress);

                        NativeMethods.git_push_transfer_progress pushProgress = pushTransferCallbacks.GenerateCallback();
                        NativeMethods.git_packbuilder_progress packBuilderProgress = packBuilderCallbacks.GenerateCallback();

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

                        Proxy.git_push_update_tips(pushHandle);
                    }
                }
                finally
                {
                    Proxy.git_remote_disconnect(remoteHandle);
                }
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
