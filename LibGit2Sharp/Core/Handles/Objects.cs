
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

    internal unsafe class BlameHandle : IDisposable
    {
        git_blame* ptr;
        internal git_blame* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe BlameHandle(git_blame* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe BlameHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_blame*) ptr.ToPointer();
            this.owned = owned;
        }

        ~BlameHandle()
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
                    NativeMethods.git_blame_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_blame*(BlameHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class DiffHandle : IDisposable
    {
        git_diff* ptr;
        internal git_diff* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe DiffHandle(git_diff* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe DiffHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_diff*) ptr.ToPointer();
            this.owned = owned;
        }

        ~DiffHandle()
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
                    NativeMethods.git_diff_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_diff*(DiffHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class PatchHandle : IDisposable
    {
        git_patch* ptr;
        internal git_patch* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe PatchHandle(git_patch* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe PatchHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_patch*) ptr.ToPointer();
            this.owned = owned;
        }

        ~PatchHandle()
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
                    NativeMethods.git_patch_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_patch*(PatchHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class ConfigurationHandle : IDisposable
    {
        git_config* ptr;
        internal git_config* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe ConfigurationHandle(git_config* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe ConfigurationHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_config*) ptr.ToPointer();
            this.owned = owned;
        }

        ~ConfigurationHandle()
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
                    NativeMethods.git_config_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_config*(ConfigurationHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class ConflictIteratorHandle : IDisposable
    {
        git_index_conflict_iterator* ptr;
        internal git_index_conflict_iterator* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe ConflictIteratorHandle(git_index_conflict_iterator* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe ConflictIteratorHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_index_conflict_iterator*) ptr.ToPointer();
            this.owned = owned;
        }

        ~ConflictIteratorHandle()
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
                    NativeMethods.git_index_conflict_iterator_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_index_conflict_iterator*(ConflictIteratorHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class IndexHandle : IDisposable
    {
        git_index* ptr;
        internal git_index* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe IndexHandle(git_index* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe IndexHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_index*) ptr.ToPointer();
            this.owned = owned;
        }

        ~IndexHandle()
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
                    NativeMethods.git_index_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_index*(IndexHandle handle)
        {
            return handle.Handle;
        }
    }

}
