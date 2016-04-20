﻿
using System;

namespace LibGit2Sharp.Core.Handles
{

    internal unsafe class TreeEntryHandle : Libgit2Object
    {
        internal TreeEntryHandle(git_tree_entry *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal TreeEntryHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_tree_entry_free((git_tree_entry*) ptr);
        }

        public static implicit operator git_tree_entry*(TreeEntryHandle handle)
        {
            return (git_tree_entry*) handle.Handle;
        }
    }

    internal unsafe class ReferenceHandle : Libgit2Object
    {
        internal ReferenceHandle(git_reference *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal ReferenceHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_reference_free((git_reference*) ptr);
        }

        public static implicit operator git_reference*(ReferenceHandle handle)
        {
            return (git_reference*) handle.Handle;
        }
    }

    internal unsafe class RepositoryHandle : Libgit2Object
    {
        internal RepositoryHandle(git_repository *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal RepositoryHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_repository_free((git_repository*) ptr);
        }

        public static implicit operator git_repository*(RepositoryHandle handle)
        {
            return (git_repository*) handle.Handle;
        }
    }

    internal unsafe class SignatureHandle : Libgit2Object
    {
        internal SignatureHandle(git_signature *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal SignatureHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_signature_free((git_signature*) ptr);
        }

        public static implicit operator git_signature*(SignatureHandle handle)
        {
            return (git_signature*) handle.Handle;
        }
    }

    internal unsafe class StatusListHandle : Libgit2Object
    {
        internal StatusListHandle(git_status_list *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal StatusListHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_status_list_free((git_status_list*) ptr);
        }

        public static implicit operator git_status_list*(StatusListHandle handle)
        {
            return (git_status_list*) handle.Handle;
        }
    }

    internal unsafe class BlameHandle : Libgit2Object
    {
        internal BlameHandle(git_blame *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal BlameHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_blame_free((git_blame*) ptr);
        }

        public static implicit operator git_blame*(BlameHandle handle)
        {
            return (git_blame*) handle.Handle;
        }
    }

    internal unsafe class DiffHandle : Libgit2Object
    {
        internal DiffHandle(git_diff *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal DiffHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_diff_free((git_diff*) ptr);
        }

        public static implicit operator git_diff*(DiffHandle handle)
        {
            return (git_diff*) handle.Handle;
        }
    }

    internal unsafe class PatchHandle : Libgit2Object
    {
        internal PatchHandle(git_patch *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal PatchHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_patch_free((git_patch*) ptr);
        }

        public static implicit operator git_patch*(PatchHandle handle)
        {
            return (git_patch*) handle.Handle;
        }
    }

    internal unsafe class ConfigurationHandle : Libgit2Object
    {
        internal ConfigurationHandle(git_config *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal ConfigurationHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_config_free((git_config*) ptr);
        }

        public static implicit operator git_config*(ConfigurationHandle handle)
        {
            return (git_config*) handle.Handle;
        }
    }

    internal unsafe class ConflictIteratorHandle : Libgit2Object
    {
        internal ConflictIteratorHandle(git_index_conflict_iterator *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal ConflictIteratorHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_index_conflict_iterator_free((git_index_conflict_iterator*) ptr);
        }

        public static implicit operator git_index_conflict_iterator*(ConflictIteratorHandle handle)
        {
            return (git_index_conflict_iterator*) handle.Handle;
        }
    }

    internal unsafe class IndexHandle : Libgit2Object
    {
        internal IndexHandle(git_index *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal IndexHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_index_free((git_index*) ptr);
        }

        public static implicit operator git_index*(IndexHandle handle)
        {
            return (git_index*) handle.Handle;
        }
    }

    internal unsafe class ReflogHandle : Libgit2Object
    {
        internal ReflogHandle(git_reflog *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal ReflogHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_reflog_free((git_reflog*) ptr);
        }

        public static implicit operator git_reflog*(ReflogHandle handle)
        {
            return (git_reflog*) handle.Handle;
        }
    }

    internal unsafe class TreeBuilderHandle : Libgit2Object
    {
        internal TreeBuilderHandle(git_treebuilder *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal TreeBuilderHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_treebuilder_free((git_treebuilder*) ptr);
        }

        public static implicit operator git_treebuilder*(TreeBuilderHandle handle)
        {
            return (git_treebuilder*) handle.Handle;
        }
    }

    internal unsafe class PackBuilderHandle : Libgit2Object
    {
        internal PackBuilderHandle(git_packbuilder *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal PackBuilderHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_packbuilder_free((git_packbuilder*) ptr);
        }

        public static implicit operator git_packbuilder*(PackBuilderHandle handle)
        {
            return (git_packbuilder*) handle.Handle;
        }
    }

    internal unsafe class NoteHandle : Libgit2Object
    {
        internal NoteHandle(git_note *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal NoteHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_note_free((git_note*) ptr);
        }

        public static implicit operator git_note*(NoteHandle handle)
        {
            return (git_note*) handle.Handle;
        }
    }

    internal unsafe class DescribeResultHandle : Libgit2Object
    {
        internal DescribeResultHandle(git_describe_result *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal DescribeResultHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_describe_result_free((git_describe_result*) ptr);
        }

        public static implicit operator git_describe_result*(DescribeResultHandle handle)
        {
            return (git_describe_result*) handle.Handle;
        }
    }

    internal unsafe class SubmoduleHandle : Libgit2Object
    {
        internal SubmoduleHandle(git_submodule *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal SubmoduleHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_submodule_free((git_submodule*) ptr);
        }

        public static implicit operator git_submodule*(SubmoduleHandle handle)
        {
            return (git_submodule*) handle.Handle;
        }
    }

    internal unsafe class AnnotatedCommitHandle : Libgit2Object
    {
        internal AnnotatedCommitHandle(git_annotated_commit *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal AnnotatedCommitHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_annotated_commit_free((git_annotated_commit*) ptr);
        }

        public static implicit operator git_annotated_commit*(AnnotatedCommitHandle handle)
        {
            return (git_annotated_commit*) handle.Handle;
        }
    }

    internal unsafe class ObjectDatabaseHandle : Libgit2Object
    {
        internal ObjectDatabaseHandle(git_odb *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal ObjectDatabaseHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_odb_free((git_odb*) ptr);
        }

        public static implicit operator git_odb*(ObjectDatabaseHandle handle)
        {
            return (git_odb*) handle.Handle;
        }
    }

    internal unsafe class RevWalkerHandle : Libgit2Object
    {
        internal RevWalkerHandle(git_revwalk *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal RevWalkerHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_revwalk_free((git_revwalk*) ptr);
        }

        public static implicit operator git_revwalk*(RevWalkerHandle handle)
        {
            return (git_revwalk*) handle.Handle;
        }
    }

    internal unsafe class RemoteHandle : Libgit2Object
    {
        internal RemoteHandle(git_remote *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal RemoteHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_remote_free((git_remote*) ptr);
        }

        public static implicit operator git_remote*(RemoteHandle handle)
        {
            return (git_remote*) handle.Handle;
        }
    }

    internal unsafe class ObjectHandle : Libgit2Object
    {
        internal ObjectHandle(git_object *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal ObjectHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_object_free((git_object*) ptr);
        }

        public static implicit operator git_object*(ObjectHandle handle)
        {
            return (git_object*) handle.Handle;
        }
    }

    internal unsafe class RebaseHandle : Libgit2Object
    {
        internal RebaseHandle(git_rebase *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal RebaseHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_rebase_free((git_rebase*) ptr);
        }

        public static implicit operator git_rebase*(RebaseHandle handle)
        {
            return (git_rebase*) handle.Handle;
        }
    }

    internal unsafe class OdbStreamHandle : Libgit2Object
    {
        internal OdbStreamHandle(git_odb_stream *ptr, bool owned)
            : base((void *) ptr, owned)
        {
        }

        internal OdbStreamHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        public override void Free()
        {
            NativeMethods.git_odb_stream_free((git_odb_stream*) ptr);
        }

        public static implicit operator git_odb_stream*(OdbStreamHandle handle)
        {
            return (git_odb_stream*) handle.Handle;
        }
    }

}
