
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
            NativeMethods.git_tree_entry_free((git_tree_entry*)handle);

            return true;
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
            NativeMethods.git_reference_free((git_reference*)handle);

            return true;
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
            NativeMethods.git_repository_free((git_repository*)handle);

            return true;
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
            NativeMethods.git_signature_free((git_signature*)handle);

            return true;
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
            NativeMethods.git_status_list_free((git_status_list*)handle);

            return true;
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
            NativeMethods.git_blame_free((git_blame*)handle);

            return true;
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
            NativeMethods.git_diff_free((git_diff*)handle);

            return true;
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
            NativeMethods.git_patch_free((git_patch*)handle);

            return true;
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
            NativeMethods.git_config_free((git_config*)handle);

            return true;
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
            NativeMethods.git_index_conflict_iterator_free((git_index_conflict_iterator*)handle);

            return true;
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
            NativeMethods.git_index_free((git_index*)handle);

            return true;
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
            NativeMethods.git_reflog_free((git_reflog*)handle);

            return true;
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
            NativeMethods.git_treebuilder_free((git_treebuilder*)handle);

            return true;
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
            NativeMethods.git_packbuilder_free((git_packbuilder*)handle);

            return true;
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
            NativeMethods.git_note_free((git_note*)handle);

            return true;
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
            NativeMethods.git_describe_result_free((git_describe_result*)handle);

            return true;
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
            NativeMethods.git_submodule_free((git_submodule*)handle);

            return true;
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
            NativeMethods.git_annotated_commit_free((git_annotated_commit*)handle);

            return true;
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
            NativeMethods.git_odb_free((git_odb*)handle);

            return true;
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
            NativeMethods.git_revwalk_free((git_revwalk*)handle);

            return true;
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
            NativeMethods.git_remote_free((git_remote*)handle);

            return true;
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
            NativeMethods.git_object_free((git_object*)handle);

            return true;
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
            NativeMethods.git_rebase_free((git_rebase*)handle);

            return true;
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
            NativeMethods.git_odb_stream_free((git_odb_stream*)handle);

            return true;
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
            NativeMethods.git_worktree_free((git_worktree*)handle);

            return true;
        }
    }

}
