
using System;

namespace LibGit2Sharp.Core
{

    internal unsafe class TreeEntryHandle : IDisposable
    {
        git_tree_entry* ptr;
        internal git_tree_entry* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe TreeEntryHandle(git_tree_entry* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe TreeEntryHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_tree_entry*) ptr.ToPointer();
            this.owned = owned;
        }

        ~TreeEntryHandle()
        {
            Dispose(false);
        }

        internal bool IsNull
        {
            get
            {
                return ptr == null;
            }
        }


        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (owned)
                {
                    NativeMethods.git_tree_entry_free(ptr);
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }

    internal unsafe class ReferenceHandle : IDisposable
    {
        git_reference* ptr;
        internal git_reference* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe ReferenceHandle(git_reference* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe ReferenceHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_reference*) ptr.ToPointer();
            this.owned = owned;
        }

        ~ReferenceHandle()
        {
            Dispose(false);
        }

        internal bool IsNull
        {
            get
            {
                return ptr == null;
            }
        }


        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (owned)
                {
                    NativeMethods.git_reference_free(ptr);
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
