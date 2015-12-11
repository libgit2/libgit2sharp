using System;

namespace LibGit2Sharp.Core.Handles
{
    internal unsafe class TreeEntryOwnedHandle : IDisposable
    {
        internal git_tree_entry* Handle { get; set; }

        internal TreeEntryOwnedHandle(git_tree_entry* entry)
        {
            Handle = entry;
        }

        public void Dispose()
        {
            Proxy.git_tree_entry_free(Handle);
        }
    }
}
