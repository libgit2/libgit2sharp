using System;

namespace LibGit2Sharp.Core.Handles;

internal unsafe class UnownedTreeEntryHandle : TreeEntryHandle
{
    internal UnownedTreeEntryHandle()
        : base(IntPtr.Zero, false)
    {
    }

    internal UnownedTreeEntryHandle(IntPtr ptr)
        : base(ptr, false)
    {
    }
}
