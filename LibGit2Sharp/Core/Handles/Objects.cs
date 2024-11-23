
using System;

namespace LibGit2Sharp.Core.Handles
{

    internal unsafe class TreeEntryHandle : Libgit2Object
    {
        internal TreeEntryHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_tree_entry_free(this);

            return true;
        }
    }

    internal unsafe class ReferenceHandle : Libgit2Object
    {
        internal ReferenceHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_reference_free(this);

            return true;
        }
    }

    internal unsafe class RepositoryHandle : Libgit2Object
    {
        internal RepositoryHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_repository_free(this);

            return true;
        }
    }

    internal unsafe class SignatureHandle : Libgit2Object
    {
        internal SignatureHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_signature_free(this);

            return true;
        }
    }

    internal unsafe class StatusListHandle : Libgit2Object
    {
        internal StatusListHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_status_list_free(this);

            return true;
        }
    }

    internal unsafe class BlameHandle : Libgit2Object
    {
        internal BlameHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_blame_free(this);

            return true;
        }
    }

    internal unsafe class DiffHandle : Libgit2Object
    {
        internal DiffHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_diff_free(this);

            return true;
        }
    }

    internal unsafe class PatchHandle : Libgit2Object
    {
        internal PatchHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_patch_free(this);

            return true;
        }
    }

    internal unsafe class ConfigurationHandle : Libgit2Object
    {
        internal ConfigurationHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_config_free(this);

            return true;
        }
    }

    internal unsafe class ConflictIteratorHandle : Libgit2Object
    {
        internal ConflictIteratorHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_index_conflict_iterator_free(this);

            return true;
        }
    }

    internal unsafe class IndexHandle : Libgit2Object
    {
        internal IndexHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_index_free(this);

            return true;
        }
    }

    internal unsafe class ReflogHandle : Libgit2Object
    {
        internal ReflogHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_reflog_free(this);

            return true;
        }
    }

    internal unsafe class TreeBuilderHandle : Libgit2Object
    {
        internal TreeBuilderHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_treebuilder_free(this);

            return true;
        }
    }

    internal unsafe class PackBuilderHandle : Libgit2Object
    {
        internal PackBuilderHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_packbuilder_free(this);

            return true;
        }
    }

    internal unsafe class NoteHandle : Libgit2Object
    {
        internal NoteHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_note_free(this);

            return true;
        }
    }

    internal unsafe class DescribeResultHandle : Libgit2Object
    {
        internal DescribeResultHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_describe_result_free(this);

            return true;
        }
    }

    internal unsafe class SubmoduleHandle : Libgit2Object
    {
        internal SubmoduleHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_submodule_free(this);

            return true;
        }
    }

    internal unsafe class AnnotatedCommitHandle : Libgit2Object
    {
        internal AnnotatedCommitHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_annotated_commit_free(this);

            return true;
        }
    }

    internal unsafe class ObjectDatabaseHandle : Libgit2Object
    {
        internal ObjectDatabaseHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_odb_free(this);

            return true;
        }
    }

    internal unsafe class RevWalkerHandle : Libgit2Object
    {
        internal RevWalkerHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_revwalk_free(this);

            return true;
        }
    }

    internal unsafe class RemoteHandle : Libgit2Object
    {
        internal RemoteHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_remote_free(this);

            return true;
        }
    }

    internal unsafe class ObjectHandle : Libgit2Object
    {
        internal ObjectHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_object_free(this);

            return true;
        }
    }

    internal unsafe class RebaseHandle : Libgit2Object
    {
        internal RebaseHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_rebase_free(this);

            return true;
        }
    }

    internal unsafe class OdbStreamHandle : Libgit2Object
    {
        internal OdbStreamHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_odb_stream_free(this);

            return true;
        }
    }

    internal unsafe class WorktreeHandle : Libgit2Object
    {
        internal WorktreeHandle(IntPtr ptr, bool owned)
            : base(ptr, owned)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_worktree_free(this);

            return true;
        }
    }

}
