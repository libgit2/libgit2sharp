
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

        internal IntPtr AsIntPtr()
        {
            return new IntPtr(ptr);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (owned)
                {
                    NativeMethods.git_tree_entry_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_tree_entry*(TreeEntryHandle handle)
        {
            return handle.Handle;
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

        internal IntPtr AsIntPtr()
        {
            return new IntPtr(ptr);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (owned)
                {
                    NativeMethods.git_reference_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_reference*(ReferenceHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class RepositoryHandle : IDisposable
    {
        git_repository* ptr;
        internal git_repository* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe RepositoryHandle(git_repository* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe RepositoryHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_repository*) ptr.ToPointer();
            this.owned = owned;
        }

        ~RepositoryHandle()
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

        internal IntPtr AsIntPtr()
        {
            return new IntPtr(ptr);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (owned)
                {
                    NativeMethods.git_repository_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_repository*(RepositoryHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class SignatureHandle : IDisposable
    {
        git_signature* ptr;
        internal git_signature* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe SignatureHandle(git_signature* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe SignatureHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_signature*) ptr.ToPointer();
            this.owned = owned;
        }

        ~SignatureHandle()
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

        internal IntPtr AsIntPtr()
        {
            return new IntPtr(ptr);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (owned)
                {
                    NativeMethods.git_signature_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_signature*(SignatureHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class StatusListHandle : IDisposable
    {
        git_status_list* ptr;
        internal git_status_list* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe StatusListHandle(git_status_list* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe StatusListHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_status_list*) ptr.ToPointer();
            this.owned = owned;
        }

        ~StatusListHandle()
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

        internal IntPtr AsIntPtr()
        {
            return new IntPtr(ptr);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (owned)
                {
                    NativeMethods.git_status_list_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_status_list*(StatusListHandle handle)
        {
            return handle.Handle;
        }
    }

}
