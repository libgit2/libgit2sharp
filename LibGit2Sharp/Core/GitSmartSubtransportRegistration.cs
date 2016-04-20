using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitSmartSubtransportRegistration
    {
        public IntPtr SubtransportCallback;
        public uint Rpc;
        public uint Param;

        public delegate int create_callback(
            out IntPtr subtransport,
            IntPtr owner,
            IntPtr param);
    }
}
