
using System;

namespace LibGit2Sharp.Core.Handles
{

    internal unsafe class TreeEntryHandle : Libgit2Object
    {
        internal TreeEntryHandle(git_tree_entry* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal TreeEntryHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_tree_entry_free((git_tree_entry*)AsIntPtr());

            return true;
        }

        public static implicit operator git_tree_entry*(TreeEntryHandle handle)
        {
            return (git_tree_entry*)handle.AsIntPtr();
        }
    }

    internal unsafe class ReferenceHandle : Libgit2Object
    {
        internal ReferenceHandle(git_reference* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal ReferenceHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_reference_free((git_reference*)AsIntPtr());

            return true;
        }

        public static implicit operator git_reference*(ReferenceHandle handle)
        {
            return (git_reference*)handle.AsIntPtr();
        }
    }

    internal unsafe class RepositoryHandle : Libgit2Object
    {
        internal RepositoryHandle(git_repository* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal RepositoryHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_repository_free((git_repository*)AsIntPtr());

            return true;
        }

        public static implicit operator git_repository*(RepositoryHandle handle)
        {
            return (git_repository*)handle.AsIntPtr();
        }
    }

    internal unsafe class SignatureHandle : Libgit2Object
    {
        internal SignatureHandle(git_signature* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal SignatureHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_signature_free((git_signature*)AsIntPtr());

            return true;
        }

        public static implicit operator git_signature*(SignatureHandle handle)
        {
            return (git_signature*)handle.AsIntPtr();
        }
    }

    internal unsafe class StatusListHandle : Libgit2Object
    {
        internal StatusListHandle(git_status_list* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal StatusListHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_status_list_free((git_status_list*)AsIntPtr());

            return true;
        }

        public static implicit operator git_status_list*(StatusListHandle handle)
        {
            return (git_status_list*)handle.AsIntPtr();
        }
    }

    internal unsafe class BlameHandle : Libgit2Object
    {
        internal BlameHandle(git_blame* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal BlameHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_blame_free((git_blame*)AsIntPtr());

            return true;
        }

        public static implicit operator git_blame*(BlameHandle handle)
        {
            return (git_blame*)handle.AsIntPtr();
        }
    }

    internal unsafe class DiffHandle : Libgit2Object
    {
        internal DiffHandle(git_diff* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal DiffHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_diff_free((git_diff*)AsIntPtr());

            return true;
        }

        public static implicit operator git_diff*(DiffHandle handle)
        {
            return (git_diff*)handle.AsIntPtr();
        }
    }

    internal unsafe class PatchHandle : Libgit2Object
    {
        internal PatchHandle(git_patch* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal PatchHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_patch_free((git_patch*)AsIntPtr());

            return true;
        }

        public static implicit operator git_patch*(PatchHandle handle)
        {
            return (git_patch*)handle.AsIntPtr();
        }
    }

    internal unsafe class ConfigurationHandle : Libgit2Object
    {
        internal ConfigurationHandle(git_config* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal ConfigurationHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_config_free((git_config*)AsIntPtr());

            return true;
        }

        public static implicit operator git_config*(ConfigurationHandle handle)
        {
            return (git_config*)handle.AsIntPtr();
        }
    }

    internal unsafe class ConflictIteratorHandle : Libgit2Object
    {
        internal ConflictIteratorHandle(git_index_conflict_iterator* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal ConflictIteratorHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_index_conflict_iterator_free((git_index_conflict_iterator*)AsIntPtr());

            return true;
        }

        public static implicit operator git_index_conflict_iterator*(ConflictIteratorHandle handle)
        {
            return (git_index_conflict_iterator*)handle.AsIntPtr();
        }
    }

    internal unsafe class IndexHandle : Libgit2Object
    {
        internal IndexHandle(git_index* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal IndexHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_index_free((git_index*)AsIntPtr());

            return true;
        }

        public static implicit operator git_index*(IndexHandle handle)
        {
            return (git_index*)handle.AsIntPtr();
        }
    }

    internal unsafe class ReflogHandle : Libgit2Object
    {
        internal ReflogHandle(git_reflog* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal ReflogHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_reflog_free((git_reflog*)AsIntPtr());

            return true;
        }

        public static implicit operator git_reflog*(ReflogHandle handle)
        {
            return (git_reflog*)handle.AsIntPtr();
        }
    }

    internal unsafe class TreeBuilderHandle : Libgit2Object
    {
        internal TreeBuilderHandle(git_treebuilder* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal TreeBuilderHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_treebuilder_free((git_treebuilder*)AsIntPtr());

            return true;
        }

        public static implicit operator git_treebuilder*(TreeBuilderHandle handle)
        {
            return (git_treebuilder*)handle.AsIntPtr();
        }
    }

    internal unsafe class PackBuilderHandle : Libgit2Object
    {
        internal PackBuilderHandle(git_packbuilder* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal PackBuilderHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_packbuilder_free((git_packbuilder*)AsIntPtr());

            return true;
        }

        public static implicit operator git_packbuilder*(PackBuilderHandle handle)
        {
            return (git_packbuilder*)handle.AsIntPtr();
        }
    }

    internal unsafe class NoteHandle : Libgit2Object
    {
        internal NoteHandle(git_note* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal NoteHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_note_free((git_note*)AsIntPtr());

            return true;
        }

        public static implicit operator git_note*(NoteHandle handle)
        {
            return (git_note*)handle.AsIntPtr();
        }
    }

    internal unsafe class DescribeResultHandle : Libgit2Object
    {
        internal DescribeResultHandle(git_describe_result* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal DescribeResultHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_describe_result_free((git_describe_result*)AsIntPtr());

            return true;
        }

        public static implicit operator git_describe_result*(DescribeResultHandle handle)
        {
            return (git_describe_result*)handle.AsIntPtr();
        }
    }

    internal unsafe class SubmoduleHandle : Libgit2Object
    {
        internal SubmoduleHandle(git_submodule* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal SubmoduleHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_submodule_free((git_submodule*)AsIntPtr());

            return true;
        }

        public static implicit operator git_submodule*(SubmoduleHandle handle)
        {
            return (git_submodule*)handle.AsIntPtr();
        }
    }

    internal unsafe class AnnotatedCommitHandle : Libgit2Object
    {
        internal AnnotatedCommitHandle(git_annotated_commit* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal AnnotatedCommitHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_annotated_commit_free((git_annotated_commit*)AsIntPtr());

            return true;
        }

        public static implicit operator git_annotated_commit*(AnnotatedCommitHandle handle)
        {
            return (git_annotated_commit*)handle.AsIntPtr();
        }
    }

    internal unsafe class ObjectDatabaseHandle : Libgit2Object
    {
        internal ObjectDatabaseHandle(git_odb* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal ObjectDatabaseHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_odb_free((git_odb*)AsIntPtr());

            return true;
        }

        public static implicit operator git_odb*(ObjectDatabaseHandle handle)
        {
            return (git_odb*)handle.AsIntPtr();
        }
    }

    internal unsafe class RevWalkerHandle : Libgit2Object
    {
        internal RevWalkerHandle(git_revwalk* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal RevWalkerHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_revwalk_free((git_revwalk*)AsIntPtr());

            return true;
        }

        public static implicit operator git_revwalk*(RevWalkerHandle handle)
        {
            return (git_revwalk*)handle.AsIntPtr();
        }
    }

    internal unsafe class RemoteHandle : Libgit2Object
    {
        internal RemoteHandle(git_remote* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal RemoteHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_remote_free((git_remote*)AsIntPtr());

            return true;
        }

        public static implicit operator git_remote*(RemoteHandle handle)
        {
            return (git_remote*)handle.AsIntPtr();
        }
    }

    internal unsafe class ObjectHandle : Libgit2Object
    {
        internal ObjectHandle(git_object* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal ObjectHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_object_free((git_object*)AsIntPtr());

            return true;
        }

        public static implicit operator git_object*(ObjectHandle handle)
        {
            return (git_object*)handle.AsIntPtr();
        }
    }

    internal unsafe class RebaseHandle : Libgit2Object
    {
        internal RebaseHandle(git_rebase* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal RebaseHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_rebase_free((git_rebase*)AsIntPtr());

            return true;
        }

        public static implicit operator git_rebase*(RebaseHandle handle)
        {
            return (git_rebase*)handle.AsIntPtr();
        }
    }

    internal unsafe class OdbStreamHandle : Libgit2Object
    {
        internal OdbStreamHandle(git_odb_stream* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal OdbStreamHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_odb_stream_free((git_odb_stream*)AsIntPtr());

            return true;
        }

        public static implicit operator git_odb_stream*(OdbStreamHandle handle)
        {
            return (git_odb_stream*)handle.AsIntPtr();
        }
    }

    internal unsafe class WorktreeHandle : Libgit2Object
    {
        internal WorktreeHandle(git_worktree* ptr, bool owned)
            : base(ptr, owned)
        {
        }

        internal WorktreeHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_worktree_free((git_worktree*)AsIntPtr());

            return true;
        }

        public static implicit operator git_worktree*(WorktreeHandle handle)
        {
            return (git_worktree*)handle.AsIntPtr();
        }
    }

}
