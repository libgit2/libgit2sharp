using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    ///   Managed structure corresponding to git_transfer_progress native structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitTransferProgress
    {
        public uint total_objects;
        public uint indexed_objects;
        public uint received_objects;
        public UIntPtr received_bytes;
    }
}
