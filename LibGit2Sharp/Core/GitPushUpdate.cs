using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitPushUpdate
    {
        public IntPtr src_refname;
        public IntPtr dst_refname;
        public GitOid src;
        public GitOid dst;
    }
}
