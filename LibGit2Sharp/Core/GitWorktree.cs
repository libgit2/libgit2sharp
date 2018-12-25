using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Core
{
    /**
    * Flags which can be passed to git_worktree_prune to alter its
    * behavior.
    */
    [Flags]
    internal enum GitWorktreePruneOptionFlags : uint
    {
        /// <summary>
        /// Prune working tree even if working tree is valid
        /// </summary>
        GIT_WORKTREE_PRUNE_VALID = (1u << 0),

        /// <summary>
        /// Prune working tree even if it is locked
        /// </summary>
        GIT_WORKTREE_PRUNE_LOCKED = (1u << 1),

        /// <summary>
        /// Prune checked out working tree
        /// </summary>
        GIT_WORKTREE_PRUNE_WORKING_TREE = (1u << 2)
    }


    [StructLayout(LayoutKind.Sequential)]
    internal class git_worktree_add_options
    {
        public uint version = 1;
        
        public int locked;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class git_worktree_prune_options
    {
        public uint version = 1;

        public GitWorktreePruneOptionFlags flags;
    }
}
