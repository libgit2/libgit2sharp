using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitRebaseOperation
    {
        internal RebaseStepOperation type;
        internal GitOid id;
        internal IntPtr exec;
    }
}
