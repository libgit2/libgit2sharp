using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Structure for git_remote_callbacks
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitRemoteCallbacks
    {
        internal uint version;

        internal NativeMethods.remote_progress_callback progress;

        internal NativeMethods.remote_completion_callback completion;

        internal NativeMethods.git_cred_acquire_cb acquire_credentials;

        internal NativeMethods.git_transport_certificate_check_cb certificate_check;

        internal NativeMethods.git_transfer_progress_callback download_progress;

        internal NativeMethods.remote_update_tips_callback update_tips;

        internal NativeMethods.git_packbuilder_progress pack_progress;

        internal NativeMethods.git_push_transfer_progress push_transfer_progress;

        internal NativeMethods.push_update_reference_callback push_update_reference;

        internal NativeMethods.push_negotiation_callback push_negotiation;

        internal IntPtr transport;

        internal IntPtr payload;
    }
}
