using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitIndexReucEntry
    {
        public uint AncestorMode;
        public uint OurMode;
        public uint TheirMode;
        public GitOid AncestorId;
        public GitOid OurId;
        public GitOid TheirId;
        public IntPtr Path;
    }
}
