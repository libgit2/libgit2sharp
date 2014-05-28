using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitSmartSubtransportDefinition
    {
        public IntPtr SubtransportCallback;
        public uint Rpc;
    }
}
