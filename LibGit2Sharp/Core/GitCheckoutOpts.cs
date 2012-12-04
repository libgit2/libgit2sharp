using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal delegate int conflict_cb(
        IntPtr conflicting_path,
        ref GitOid blob_oid,
        uint index_mode,
        uint wd_mode,
        IntPtr payload);

    internal delegate void progress_cb(
            IntPtr strPtr,
            UIntPtr completed_steps,
            UIntPtr total_steps,
            IntPtr payload);

    [StructLayout(LayoutKind.Sequential)]
    internal class GitCheckoutOpts
    {
        public uint version = 1;
        public CheckoutStrategy checkout_strategy;
        public int DisableFilters;
        public int DirMode;
        public int FileMode;
        public int FileOpenFlags;
        public conflict_cb conflictCb;
        public IntPtr ConflictPayload;
        public progress_cb ProgressCb;
        public IntPtr ProgressPayload;
        public UnSafeNativeMethods.git_strarray paths;
    }

    [Flags]
    internal enum CheckoutStrategy
    {
        /// <summary>
        ///   Default is a dry run, no actual updates.
        /// </summary>
        GIT_CHECKOUT_DEFAULT = 0,

        /// <summary>
        ///   Allow update of entries where working dir matches HEAD.
        /// </summary>
        GIT_CHECKOUT_UPDATE_UNMODIFIED = (1 << 0),

        /// <summary>
        ///   Allow update of entries where working dir does not have file.
        /// </summary>
        GIT_CHECKOUT_UPDATE_MISSING = (1 << 1),

        /// <summary>
        ///   Allow safe updates that cannot overwrite uncommited data.
        /// </summary>
        GIT_CHECKOUT_SAFE =
            (GIT_CHECKOUT_UPDATE_UNMODIFIED | GIT_CHECKOUT_UPDATE_MISSING),

        /// <summary>
        ///   Allow update of entries in working dir that are modified from HEAD.
        /// </summary>
        GIT_CHECKOUT_UPDATE_MODIFIED = (1 << 2),

        /// <summary>
        ///   Update existing untracked files that are now present in the index.
        /// </summary>
        GIT_CHECKOUT_UPDATE_UNTRACKED = (1 << 3),

        /// <summary>
        ///   Allow all updates to force working directory to look like index.
        /// </summary>
        GIT_CHECKOUT_FORCE =
            (GIT_CHECKOUT_SAFE | GIT_CHECKOUT_UPDATE_MODIFIED | GIT_CHECKOUT_UPDATE_UNTRACKED),

        /// <summary>
        ///   Allow checkout to make updates even if conflicts are found.
        /// </summary>
        GIT_CHECKOUT_ALLOW_CONFLICTS = (1 << 4),

        /// <summary>
        ///   Remove untracked files not in index (that are not ignored).
        /// </summary>
        GIT_CHECKOUT_REMOVE_UNTRACKED = (1 << 5),

        /// <summary>
        ///   Only update existing files, don't create new ones.
        /// </summary>
        GIT_CHECKOUT_UPDATE_ONLY = (1 << 6),

        /*
         * THE FOLLOWING OPTIONS ARE NOT YET IMPLEMENTED.
         */

        /// <summary>
        ///   Allow checkout to skip unmerged files (NOT IMPLEMENTED).
        /// </summary>
        GIT_CHECKOUT_SKIP_UNMERGED = (1 << 10),

        /// <summary>
        /// For unmerged files, checkout stage 2 from index (NOT IMPLEMENTED).
        /// </summary>
        GIT_CHECKOUT_USE_OURS = (1 << 11),

        /// <summary>
        ///   For unmerged files, checkout stage 3 from index (NOT IMPLEMENTED).
        /// </summary>
        GIT_CHECKOUT_USE_THEIRS = (1 << 12),

        /// <summary>
        ///   Recursively checkout submodules with same options (NOT IMPLEMENTED).
        /// </summary>
        GIT_CHECKOUT_UPDATE_SUBMODULES = (1 << 16),

        /// <summary>
        ///   Recursively checkout submodules if HEAD moved in super repo (NOT IMPLEMENTED) */
        /// </summary>
        GIT_CHECKOUT_UPDATE_SUBMODULES_IF_CHANGED = (1 << 17),
    }
}
