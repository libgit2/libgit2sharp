using System;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp.Core
{
    internal class PushTransferCallbacks
    {
        private readonly PushTransferProgressHandler onPushTransferProgress;

        /// <summary>
        /// Constructor to set up the native callback given managed delegate.
        /// </summary>
        /// <param name="onPushTransferProgress">The <see cref="TransferProgressHandler"/> delegate that the git_transfer_progress_callback will call.</param>
        internal PushTransferCallbacks(PushTransferProgressHandler onPushTransferProgress)
        {
            this.onPushTransferProgress = onPushTransferProgress;
        }

        /// <summary>
        /// Generates a delegate that matches the native git_transfer_progress_callback function's signature and wraps the <see cref="PushTransferProgressHandler"/> delegate.
        /// </summary>
        /// <returns>A delegate method with a signature that matches git_transfer_progress_callback.</returns>
        internal NativeMethods.git_push_transfer_progress GenerateCallback()
        {
            if (onPushTransferProgress == null)
            {
                return null;
            }

            return new PushTransferCallbacks(onPushTransferProgress).OnGitTransferProgress;
        }

        private int OnGitTransferProgress(uint current, uint total, UIntPtr bytes, IntPtr payload)
        {
            return Proxy.ConvertResultToCancelFlag(onPushTransferProgress((int)current, (int)total, (long)bytes));
        }
    }
}
