using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Class to handle the mapping between libgit2 git_transfer_progress_callback function and 
    ///   a corresponding <see cref = "TransferProgressHandler" />. Generates a delegate that 
    ///   wraps the <see cref = "TransferProgressHandler" /> delegate with a delegate that matches
    ///   the git_transfer_progress_callback signature.
    /// </summary>
    internal class TransferCallbacks
    {
        /// <summary>
        ///   Managed delegate to be called in response to a git_transfer_progress_callback callback from libgit2.
        /// </summary>
        private TransferProgressHandler onTransferProgress;

        /// <summary>
        ///   Constructor to set up the native callback given managed delegate.
        /// </summary>
        /// <param name="onTransferProgress">The <see cref="TransferProgressHandler"/> delegate that the git_transfer_progress_callback will call.</param>
        private TransferCallbacks(TransferProgressHandler onTransferProgress)
        {
            this.onTransferProgress = onTransferProgress;
        }

        /// <summary>
        ///   Generates a delegate that matches the native git_transfer_progress_callback function's signature and wraps the <see cref = "TransferProgressHandler" /> delegate.
        /// </summary>
        /// <param name="onTransferProgress">The <see cref = "TransferProgressHandler" /> delegate to call in responde to a the native git_transfer_progress_callback callback.</param>
        /// <returns>A delegate method with a signature that matches git_transfer_progress_callback.</returns>
        internal static NativeMethods.git_transfer_progress_callback GenerateCallback(TransferProgressHandler onTransferProgress)
        {
            if (onTransferProgress == null)
            {
                return null;
            }

            return new TransferCallbacks(onTransferProgress).OnGitTransferProgress;
        }

        /// <summary>
        ///   The delegate with the signature that matches the native git_transfer_progress_callback function's signature.
        /// </summary>
        /// <param name="progress"><see cref = "GitTransferProgress" /> structure containing progress information.</param>
        /// <param name="payload">Payload data.</param>
        private void OnGitTransferProgress(ref GitTransferProgress progress, IntPtr payload)
        {
            onTransferProgress(new TransferProgress(progress));
        }
    }
}
