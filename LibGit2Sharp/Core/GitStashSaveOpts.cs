using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GitStashSaveOpts
    {
        public GitStashSaveOpts(
            StashModifiers flags,
            git_signature* stasher,
            string message,
            GitStrArray paths
        )
        {
            Version = 1;
            Flags = flags;
            Stasher = stasher;
            Message = message;
            Paths = paths;
        }

        public uint Version;
        public StashModifiers Flags;
        public git_signature* Stasher;
        public string Message;
        public GitStrArray Paths;
    }
}
