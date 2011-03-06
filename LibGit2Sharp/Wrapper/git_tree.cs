using System.Runtime.InteropServices;

namespace LibGit2Sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_tree
    {
        public git_object tree;
        public git_vector entries;
    }
}