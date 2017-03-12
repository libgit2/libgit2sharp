using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_index_name_entry
    {
        public char* ancestor;
        public char* ours;
        public char* theirs;
    }
}
