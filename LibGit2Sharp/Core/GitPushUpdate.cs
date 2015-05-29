using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitPushUpdate
    {
        IntPtr src_refname;
        IntPtr dst_refname;
        GitOid src;
        GitOid dst;
    }
}
