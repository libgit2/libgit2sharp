
using System;

namespace LibGit2Sharp.Core
{

    internal unsafe class TreeEntryHandle : IDisposable
    {
        internal git_tree_entry* Handle { get; private set; }
        bool owned;
        bool disposed;

        public unsafe TreeEntryHandle(git_tree_entry* handle, bool owned)
        {
            this.Handle = handle;
            this.owned = owned;
        }

        ~TreeEntryHandle()
        {
            Dispose(false);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (owned)
                {
                    NativeMethods.git_tree_entry_free(Handle);
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
