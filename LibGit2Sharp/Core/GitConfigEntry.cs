using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GitConfigEntry
    {
        public char* namePtr;
        public char* valuePtr;
        public char* backend_type;
        public char* origin_path;
        public uint include_depth;
        public uint level;
        public void* freePtr;
    }
}
