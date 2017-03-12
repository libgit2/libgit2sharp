using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_index_reuc_entry
    {
        public uint AncestorMode;
        public uint OurMode;
        public uint TheirMode;
        public git_oid AncestorId;
        public git_oid OurId;
        public git_oid TheirId;
        public char* Path;
    }
}
