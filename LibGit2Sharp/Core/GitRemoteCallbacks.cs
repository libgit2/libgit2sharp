using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    ///   Structure for git_remote_callbacks
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitRemoteCallbacks
    {
        internal uint version;

        internal NativeMethods.remote_progress_callback progress;

        internal NativeMethods.remote_completion_callback completion;

        internal NativeMethods.remote_update_tips_callback update_tips;

        internal IntPtr payload;
    }
}
