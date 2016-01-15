
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

    internal unsafe class ReflogHandle : IDisposable
    {
        git_reflog* ptr;
        internal git_reflog* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe ReflogHandle(git_reflog* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe ReflogHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_reflog*) ptr.ToPointer();
            this.owned = owned;
        }

        ~ReflogHandle()
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
                    NativeMethods.git_reflog_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_reflog*(ReflogHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class TreeBuilderHandle : IDisposable
    {
        git_treebuilder* ptr;
        internal git_treebuilder* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe TreeBuilderHandle(git_treebuilder* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe TreeBuilderHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_treebuilder*) ptr.ToPointer();
            this.owned = owned;
        }

        ~TreeBuilderHandle()
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
                    NativeMethods.git_treebuilder_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_treebuilder*(TreeBuilderHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class PackBuilderHandle : IDisposable
    {
        git_packbuilder* ptr;
        internal git_packbuilder* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe PackBuilderHandle(git_packbuilder* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe PackBuilderHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_packbuilder*) ptr.ToPointer();
            this.owned = owned;
        }

        ~PackBuilderHandle()
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
                    NativeMethods.git_packbuilder_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_packbuilder*(PackBuilderHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class NoteHandle : IDisposable
    {
        git_note* ptr;
        internal git_note* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe NoteHandle(git_note* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe NoteHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_note*) ptr.ToPointer();
            this.owned = owned;
        }

        ~NoteHandle()
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
                    NativeMethods.git_note_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_note*(NoteHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class DescribeResultHandle : IDisposable
    {
        git_describe_result* ptr;
        internal git_describe_result* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe DescribeResultHandle(git_describe_result* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe DescribeResultHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_describe_result*) ptr.ToPointer();
            this.owned = owned;
        }

        ~DescribeResultHandle()
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
                    NativeMethods.git_describe_result_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_describe_result*(DescribeResultHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class SubmoduleHandle : IDisposable
    {
        git_submodule* ptr;
        internal git_submodule* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe SubmoduleHandle(git_submodule* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe SubmoduleHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_submodule*) ptr.ToPointer();
            this.owned = owned;
        }

        ~SubmoduleHandle()
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
                    NativeMethods.git_submodule_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_submodule*(SubmoduleHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class AnnotatedCommitHandle : IDisposable
    {
        git_annotated_commit* ptr;
        internal git_annotated_commit* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe AnnotatedCommitHandle(git_annotated_commit* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe AnnotatedCommitHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_annotated_commit*) ptr.ToPointer();
            this.owned = owned;
        }

        ~AnnotatedCommitHandle()
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
                    NativeMethods.git_annotated_commit_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_annotated_commit*(AnnotatedCommitHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class ObjectDatabaseHandle : IDisposable
    {
        git_odb* ptr;
        internal git_odb* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe ObjectDatabaseHandle(git_odb* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe ObjectDatabaseHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_odb*) ptr.ToPointer();
            this.owned = owned;
        }

        ~ObjectDatabaseHandle()
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
                    NativeMethods.git_odb_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_odb*(ObjectDatabaseHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class RevWalkerHandle : IDisposable
    {
        git_revwalk* ptr;
        internal git_revwalk* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe RevWalkerHandle(git_revwalk* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe RevWalkerHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_revwalk*) ptr.ToPointer();
            this.owned = owned;
        }

        ~RevWalkerHandle()
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
                    NativeMethods.git_revwalk_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_revwalk*(RevWalkerHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class RemoteHandle : IDisposable
    {
        git_remote* ptr;
        internal git_remote* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe RemoteHandle(git_remote* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe RemoteHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_remote*) ptr.ToPointer();
            this.owned = owned;
        }

        ~RemoteHandle()
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
                    NativeMethods.git_remote_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_remote*(RemoteHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class ObjectHandle : IDisposable
    {
        git_object* ptr;
        internal git_object* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe ObjectHandle(git_object* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe ObjectHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_object*) ptr.ToPointer();
            this.owned = owned;
        }

        ~ObjectHandle()
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
                    NativeMethods.git_object_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_object*(ObjectHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class RebaseHandle : IDisposable
    {
        git_rebase* ptr;
        internal git_rebase* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe RebaseHandle(git_rebase* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe RebaseHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_rebase*) ptr.ToPointer();
            this.owned = owned;
        }

        ~RebaseHandle()
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
                    NativeMethods.git_rebase_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_rebase*(RebaseHandle handle)
        {
            return handle.Handle;
        }
    }

    internal unsafe class OdbStreamHandle : IDisposable
    {
        git_odb_stream* ptr;
        internal git_odb_stream* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        public unsafe OdbStreamHandle(git_odb_stream* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        public unsafe OdbStreamHandle(IntPtr ptr, bool owned)
        {
            this.ptr = (git_odb_stream*) ptr.ToPointer();
            this.owned = owned;
        }

        ~OdbStreamHandle()
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
                    NativeMethods.git_odb_stream_free(ptr);
                    ptr = null;
                }
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public static implicit operator git_odb_stream*(OdbStreamHandle handle)
        {
            return handle.Handle;
        }
    }

}
