using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_push_update
    {
        public char* src_refname;
        public char* dst_refname;
        public git_oid src;
        public git_oid dst;
    }
}
