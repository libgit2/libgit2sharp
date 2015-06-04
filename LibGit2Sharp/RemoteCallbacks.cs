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
        internal RemoteCallbacks(CredentialsHandler credentialsProvider)
        {
            CredentialsProvider = credentialsProvider;
        }

        internal RemoteCallbacks(PushOptions pushOptions)
        {
            if (pushOptions == null)
            {
                return;
            }

            PushTransferProgress = pushOptions.OnPushTransferProgress;
            PackBuilderProgress = pushOptions.OnPackBuilderProgress;
            CredentialsProvider = pushOptions.CredentialsProvider;
            CertificateCheck = pushOptions.CertificateCheck;
            PushStatusError = pushOptions.OnPushStatusError;
            PrePushCallback = pushOptions.OnNegotiationCompletedBeforePush;
        }

        internal RemoteCallbacks(FetchOptionsBase fetchOptions)
        {
            if (fetchOptions == null)
            {
                return;
            }

            Progress = fetchOptions.OnProgress;
            DownloadTransferProgress = fetchOptions.OnTransferProgress;
            UpdateTips = fetchOptions.OnUpdateTips;
            CredentialsProvider = fetchOptions.CredentialsProvider;
            CertificateCheck = fetchOptions.CertificateCheck;
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
        /// PushStatusError callback. It will be called when the libgit2 push_update_reference returns a non null status message,
        /// which means that the update was rejected by the remote server.
        /// </summary>
        private readonly PushStatusErrorHandler PushStatusError;

        /// <summary>
        /// Managed delegate to be called in response to a git_transfer_progress_callback callback from libgit2.
        /// This will in turn call the user provided delegate.
        /// </summary>
        private readonly TransferProgressHandler DownloadTransferProgress;

        /// <summary>
        /// Push transfer progress callback.
        /// </summary>
        private readonly PushTransferProgressHandler PushTransferProgress;

        /// <summary>
        /// Pack builder creation progress callback.
        /// </summary>
        private readonly PackBuilderProgressHandler PackBuilderProgress;

        /// <summary>
        /// Called during remote push operation after negotiation, before upload
        /// </summary>
        private readonly PrePushHandler PrePushCallback;

        #endregion

        /// <summary>
        /// The credentials to use for authentication.
        /// </summary>
        private readonly CredentialsHandler CredentialsProvider;

        /// <summary>
        /// Callback to perform validation on the certificate
        /// </summary>
        private readonly CertificateCheckHandler CertificateCheck;

        internal GitRemoteCallbacks GenerateCallbacks()
        {
            var callbacks = new GitRemoteCallbacks { version = 1 };

            if (Progress != null)
            {
                callbacks.progress = GitProgressHandler;
            }

            if (UpdateTips != null)
            {
                callbacks.update_tips = GitUpdateTipsHandler;
            }

            if (PushStatusError != null)
            {
                callbacks.push_update_reference = GitPushUpdateReference;
            }

            if (CredentialsProvider != null)
            {
                callbacks.acquire_credentials = GitCredentialHandler;
            }

            if (CertificateCheck != null)
            {
                callbacks.certificate_check = GitCertificateCheck;
            }

            if (DownloadTransferProgress != null)
            {
                callbacks.download_progress = GitDownloadTransferProgressHandler;
            }

            if (PushTransferProgress != null)
            {
                callbacks.push_transfer_progress = GitPushTransferProgressHandler;
            }

            if (PackBuilderProgress != null)
            {
                callbacks.pack_progress = GitPackbuilderProgressHandler;
            }

            if (PrePushCallback != null)
            {
                callbacks.push_negotiation = GitPushNegotiationHandler;
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
        /// <returns>0 on success; a negative value to abort the process.</returns>
        private int GitProgressHandler(IntPtr str, int len, IntPtr data)
        {
            ProgressHandler onProgress = Progress;

            bool shouldContinue = true;

            if (onProgress != null)
            {
                string message = LaxUtf8Marshaler.FromNative(str, len);
                shouldContinue = onProgress(message);
            }

            return Proxy.ConvertResultToCancelFlag(shouldContinue);
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
            bool shouldContinue = true;

            if (onUpdateTips != null)
            {
                string refName = LaxUtf8Marshaler.FromNative(str);
                shouldContinue = onUpdateTips(refName, oldId, newId);
            }

            return Proxy.ConvertResultToCancelFlag(shouldContinue);
        }

        /// <summary>
        /// The delegate with the signature that matches the native push_update_reference function's signature
        /// </summary>
        /// <param name="str">IntPtr to string, the name of the reference</param>
        /// <param name="status">IntPtr to string, the update status message</param>
        /// <param name="data">IntPtr to optional payload passed back to the callback.</param>
        /// <returns>0 on success; a negative value to abort the process.</returns>
        private int GitPushUpdateReference(IntPtr str, IntPtr status, IntPtr data)
        {
            PushStatusErrorHandler onPushError = PushStatusError;

            if (onPushError != null)
            {
                string reference = LaxUtf8Marshaler.FromNative(str);
                string message = LaxUtf8Marshaler.FromNative(status);
                if (message != null)
                {
                    onPushError(new PushStatusError(reference, message));
                }
            }

            return Proxy.ConvertResultToCancelFlag(true);
        }

        /// <summary>
        /// The delegate with the signature that matches the native git_transfer_progress_callback function's signature.
        /// </summary>
        /// <param name="progress"><see cref="GitTransferProgress"/> structure containing progress information.</param>
        /// <param name="payload">Payload data.</param>
        /// <returns>the result of the wrapped <see cref="TransferProgressHandler"/></returns>
        private int GitDownloadTransferProgressHandler(ref GitTransferProgress progress, IntPtr payload)
        {
            bool shouldContinue = true;

            if (DownloadTransferProgress != null)
            {
                shouldContinue = DownloadTransferProgress(new TransferProgress(progress));
            }

            return Proxy.ConvertResultToCancelFlag(shouldContinue);
        }

        private int GitPushTransferProgressHandler(uint current, uint total, UIntPtr bytes, IntPtr payload)
        {
            bool shouldContinue = true;

            if (PushTransferProgress != null)
            {
                shouldContinue = PushTransferProgress((int)current, (int)total, (long)bytes);
            }

            return Proxy.ConvertResultToCancelFlag(shouldContinue);
        }

        private int GitPackbuilderProgressHandler(int stage, uint current, uint total, IntPtr payload)
        {
            bool shouldContinue = true;

            if (PackBuilderProgress != null)
            {
                shouldContinue = PackBuilderProgress((PackBuilderStage)stage, (int)current, (int)total);
            }

            return Proxy.ConvertResultToCancelFlag(shouldContinue);
        }

        private int GitCredentialHandler(
            out IntPtr ptr,
            IntPtr cUrl,
            IntPtr usernameFromUrl,
            GitCredentialType credTypes,
            IntPtr payload)
        {
            string url = LaxUtf8Marshaler.FromNative(cUrl);
            string username = LaxUtf8Marshaler.FromNative(usernameFromUrl);

            SupportedCredentialTypes types = default(SupportedCredentialTypes);
            if (credTypes.HasFlag(GitCredentialType.UserPassPlaintext))
            {
                types |= SupportedCredentialTypes.UsernamePassword;
            }
            if (credTypes.HasFlag(GitCredentialType.Default))
            {
                types |= SupportedCredentialTypes.Default;
            }
            if (credTypes.HasFlag(GitCredentialType.SshKey))
            {
                types |= SupportedCredentialTypes.Ssh;
            }
            if (credTypes.HasFlag(GitCredentialType.Username))
            {
                types |= SupportedCredentialTypes.UsernameQuery;
            }

            var cred = CredentialsProvider(url, username, types);

            return cred.GitCredentialHandler(out ptr);
        }

        private int GitCertificateCheck(IntPtr certPtr, int valid, IntPtr cHostname, IntPtr payload)
        {
            string hostname = LaxUtf8Marshaler.FromNative(cHostname);
            GitCertificate baseCert = certPtr.MarshalAs<GitCertificate>();
            Certificate cert = null;

            switch (baseCert.type)
            {
                case GitCertificateType.X509:
                    cert = new CertificateX509(certPtr.MarshalAs<GitCertificateX509>());
                    break;
                case GitCertificateType.Hostkey:
                    cert = new CertificateSsh(certPtr.MarshalAs<GitCertificateSsh>());
                    break;
            }

            bool result = false;
            try
            {
                result = CertificateCheck(cert, valid != 0, hostname);
            }
            catch (Exception exception)
            {
                Proxy.giterr_set_str(GitErrorCategory.Callback, exception);
            }

            return Proxy.ConvertResultToCancelFlag(result);
        }

        private int GitPushNegotiationHandler(IntPtr updates, UIntPtr len, IntPtr payload)
        {
            if (updates == IntPtr.Zero)
            {
                return (int)GitErrorCode.Error;
            }

            bool result = false;
            try
            {

                int length = len.ConvertToInt();
                PushUpdate[] pushUpdates = new PushUpdate[length];

                unsafe
                {
                    IntPtr* ptr = (IntPtr*)updates.ToPointer();

                    for (int i = 0; i < length; i++)
                    {
                        if (ptr[i] == IntPtr.Zero)
                        {
                            throw new NullReferenceException("Unexpected null git_push_update pointer was encountered");
                        }

                        GitPushUpdate gitPushUpdate = ptr[i].MarshalAs<GitPushUpdate>();
                        PushUpdate pushUpdate = new PushUpdate(gitPushUpdate);
                        pushUpdates[i] = pushUpdate;
                    }

                    result = PrePushCallback(pushUpdates);
                }
            }
            catch (Exception exception)
            {
                Log.Write(LogLevel.Error, exception.ToString());
                Proxy.giterr_set_str(GitErrorCategory.Callback, exception);
                result = false;
            }

            return Proxy.ConvertResultToCancelFlag(result);
        }

        #endregion
    }
}
