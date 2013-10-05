using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Class to translate libgit2 callbacks into delegates exposed by LibGit2Sharp.
    /// Handles generating libgit2 git_remote_callbacks datastructure given a set
    /// of LibGit2Sharp delegates and handles propagating libgit2 callbacks into
    /// corresponding LibGit2Sharp exposed delegates.
    /// </summary>
    internal class RemoteCallbacks
    {
        internal RemoteCallbacks(
            ProgressHandler onProgress = null,
            TransferProgressHandler onDownloadProgress = null,
            CompletionHandler onCompletion = null,
            UpdateTipsHandler onUpdateTips = null,
            Credentials credentials = null)
        {
            Progress = onProgress;
            DownloadTransferProgress = onDownloadProgress;
            Completion = onCompletion;
            UpdateTips = onUpdateTips;
            Credentials = credentials;
        }

        #region Delegates

        /// <summary>
        /// Progress callback. Corresponds to libgit2 progress callback.
        /// </summary>
        private readonly ProgressHandler Progress;

        /// <summary>
        /// UpdateTips callback. Corresponds to libgit2 update_tips callback.
        /// </summary>
        private readonly UpdateTipsHandler UpdateTips;

        /// <summary>
        /// Completion callback. Corresponds to libgit2 Completion callback.
        /// </summary>
        private readonly CompletionHandler Completion;

        /// <summary>
        /// Managed delegate to be called in response to a git_transfer_progress_callback callback from libgit2.
        /// This will in turn call the user provided delegate.
        /// </summary>
        private readonly TransferProgressHandler DownloadTransferProgress;

        #endregion

        /// <summary>
        /// The credentials to use for the credential callback.
        /// </summary>
        Credentials Credentials;

        internal GitRemoteCallbacks GenerateCallbacks()
        {
            var callbacks = new GitRemoteCallbacks {version = 1};

            if (Progress != null)
            {
                callbacks.progress = GitProgressHandler;
            }

            if (UpdateTips != null)
            {
                callbacks.update_tips = GitUpdateTipsHandler;
            }

            if (Completion != null)
            {
                callbacks.completion = GitCompletionHandler;
            }

            if (Credentials != null)
            {
                callbacks.acquire_credentials = GitCredentialHandler;
            }

            if (DownloadTransferProgress != null)
            {
                callbacks.download_progress = GitDownloadTransferProgressHandler;
            }

            return callbacks;
        }

        #region Handlers to respond to callbacks raised by libgit2

        /// <summary>
        /// Handler for libgit2 Progress callback. Converts values
        /// received from libgit2 callback to more suitable types
        /// and calls delegate provided by LibGit2Sharp consumer.
        /// </summary>
        /// <param name="str">IntPtr to string from libgit2</param>
        /// <param name="len">length of string</param>
        /// <param name="data">IntPtr to optional payload passed back to the callback.</param>
        private void GitProgressHandler(IntPtr str, int len, IntPtr data)
        {
            ProgressHandler onProgress = Progress;

            if (onProgress != null)
            {
                string message = Utf8Marshaler.FromNative(str, len);
                onProgress(message);
            }
        }

        /// <summary>
        /// Handler for libgit2 update_tips callback. Converts values
        /// received from libgit2 callback to more suitable types
        /// and calls delegate provided by LibGit2Sharp consumer.
        /// </summary>
        /// <param name="str">IntPtr to string</param>
        /// <param name="oldId">Old reference ID</param>
        /// <param name="newId">New referene ID</param>
        /// <param name="data">IntPtr to optional payload passed back to the callback.</param>
        /// <returns>0 on success; a negative value to abort the process.</returns>
        private int GitUpdateTipsHandler(IntPtr str, ref GitOid oldId, ref GitOid newId, IntPtr data)
        {
            UpdateTipsHandler onUpdateTips = UpdateTips;
            int result = 0;

            if (onUpdateTips != null)
            {
                string refName = Utf8Marshaler.FromNative(str);
                result = onUpdateTips(refName, oldId, newId);
            }

            return result;
        }

        /// <summary>
        /// Handler for libgit2 completion callback. Converts values
        /// received from libgit2 callback to more suitable types
        /// and calls delegate provided by LibGit2Sharp consumer.
        /// </summary>
        /// <param name="remoteCompletionType">Which operation completed.</param>
        /// <param name="data">IntPtr to optional payload passed back to the callback.</param>
        /// <returns>0 on success; a negative value to abort the process.</returns>
        private int GitCompletionHandler(RemoteCompletionType remoteCompletionType, IntPtr data)
        {
            CompletionHandler completion = Completion;
            int result = 0;

            if (completion != null)
            {
                result = completion(remoteCompletionType);
            }

            return result;
        }

        /// <summary>
        /// The delegate with the signature that matches the native git_transfer_progress_callback function's signature.
        /// </summary>
        /// <param name="progress"><see cref="GitTransferProgress"/> structure containing progress information.</param>
        /// <param name="payload">Payload data.</param>
        /// <returns>the result of the wrapped <see cref="TransferProgressHandler"/></returns>
        private int GitDownloadTransferProgressHandler(ref GitTransferProgress progress, IntPtr payload)
        {
            int result = 0;

            if (DownloadTransferProgress != null)
            {
                result = DownloadTransferProgress(new TransferProgress(progress));
            }

            return result;
        }

        private int GitCredentialHandler(out IntPtr cred, IntPtr url, IntPtr username_from_url, uint types, IntPtr payload)
        {
            return NativeMethods.git_cred_userpass_plaintext_new(out cred, Credentials.Username, Credentials.Password);
        }

        #endregion
    }
}
