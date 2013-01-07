using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum CheckoutStrategy
    {
        /// <summary>
        ///   Default is a dry run, no actual updates.
        /// </summary>
        GIT_CHECKOUT_NONE = 0,

        /// <summary>
        ///   Allow safe updates that cannot overwrite uncommited data.
        /// </summary>
        GIT_CHECKOUT_SAFE = (1 << 0),

        /// <summary>
        ///   Allow safe updates plus creation of missing files.
        /// </summary>
        GIT_CHECKOUT_SAFE_CREATE = (1 << 1),

        /// <summary>
        ///   Allow update of entries in working dir that are modified from HEAD.
        /// </summary>
        GIT_CHECKOUT_FORCE = (1 << 2),

        /// <summary>
        /// Allow checkout to make safe updates even if conflicts are found
        /// </summary>
        GIT_CHECKOUT_ALLOW_CONFLICTS = (1 << 4),

        /// <summary>
        /// Remove untracked files not in index (that are not ignored)
        /// </summary>
        GIT_CHECKOUT_REMOVE_UNTRACKED = (1 << 5),

        /// <summary>
        /// Remove ignored files not in index
        /// </summary>
        GIT_CHECKOUT_REMOVE_IGNORED = (1 << 6),

        /// <summary>
        /// Only update existing files, don't create new ones
        /// </summary>
        GIT_CHECKOUT_UPDATE_ONLY = (1 << 7),

        /// <summary>
        /// Normally checkout updates index entries as it goes; this stops that
        /// </summary>
        GIT_CHECKOUT_DONT_UPDATE_INDEX = (1 << 8),

        /// <summary>
        /// Don't refresh index/config/etc before doing checkout
        /// </summary>
        GIT_CHECKOUT_NO_REFRESH = (1 << 9),

        ///Allow checkout to skip unmerged files (NOT IMPLEMENTED)
        GIT_CHECKOUT_SKIP_UNMERGED = (1 << 10),

        /// <summary>
        /// For unmerged files, checkout stage 2 from index (NOT IMPLEMENTED)
        /// </summary>
        GIT_CHECKOUT_USE_OURS = (1 << 11),

        /// <summary>
        /// For unmerged files, checkout stage 3 from index (NOT IMPLEMENTED)
        /// </summary>
        GIT_CHECKOUT_USE_THEIRS = (1 << 12),

        /// <summary>
        /// Recursively checkout submodules with same options (NOT IMPLEMENTED)
        /// </summary>
        GIT_CHECKOUT_UPDATE_SUBMODULES = (1 << 16),

        /// <summary>
        /// Recursively checkout submodules if HEAD moved in super repo (NOT IMPLEMENTED)
        /// </summary>
        GIT_CHECKOUT_UPDATE_SUBMODULES_IF_CHANGED = (1 << 17),
    }

    [Flags]
    internal enum NotifyFlags
    {
        GIT_CHECKOUT_NOTIFY_NONE = 0,
        GIT_CHECKOUT_NOTIFY_CONFLICT = (1 << 0),
        GIT_CHECKOUT_NOTIFY_DIRTY = (1 << 1),
        GIT_CHECKOUT_NOTIFY_UPDATED = (1 << 2),
        GIT_CHECKOUT_NOTIFY_UNTRACKED = (1 << 3),
        GIT_CHECKOUT_NOTIFY_IGNORED = (1 << 4),
    }

    internal delegate int checkout_notify_cb(
        NotifyFlags why,
        IntPtr path,
        IntPtr baseline,
        IntPtr target,
        IntPtr workdir,
        IntPtr payload);

    internal delegate void progress_cb(
            IntPtr strPtr,
            UIntPtr completed_steps,
            UIntPtr total_steps,
            IntPtr payload);

    [StructLayout(LayoutKind.Sequential)]
    internal struct GitCheckoutOpts
    {
        public uint version;

        public CheckoutStrategy checkout_strategy;

        public int DisableFilters;
        public uint DirMode;
        public uint FileMode;
        public int FileOpenFlags;

        public NotifyFlags notify_flags;
        public checkout_notify_cb notify_cb;
        public IntPtr notify_payload;

        public progress_cb progress_cb;
        public IntPtr progress_payload;

        public UnSafeNativeMethods.git_strarray paths;

        public IntPtr baseline;
    }
}
