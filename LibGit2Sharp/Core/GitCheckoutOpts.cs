using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum CheckoutStrategy
    {
        /**
	     * Allow safe updates that cannot overwrite uncommitted data.
	     * If the uncommitted changes don't conflict with the checked
	     * out files, the checkout will still proceed, leaving the
	     * changes intact.
	     */
        GIT_CHECKOUT_SAFE = 0,

        /**
         * Allow all updates to force working directory to look like
         * the index, potentially losing data in the process.
         */
        GIT_CHECKOUT_FORCE = (1 << 1),

        /** Allow checkout to recreate missing files */
        GIT_CHECKOUT_RECREATE_MISSING = (1 << 2),

        /** Allow checkout to make safe updates even if conflicts are found */
        GIT_CHECKOUT_ALLOW_CONFLICTS = (1 << 4),

        /** Remove untracked files not in index (that are not ignored) */
        GIT_CHECKOUT_REMOVE_UNTRACKED = (1 << 5),

        /** Remove ignored files not in index */
        GIT_CHECKOUT_REMOVE_IGNORED = (1 << 6),

        /** Only update existing files, don't create new ones */
        GIT_CHECKOUT_UPDATE_ONLY = (1 << 7),

        /**
         * Normally checkout updates index entries as it goes; this stops that.
         * Implies `GIT_CHECKOUT_DONT_WRITE_INDEX`.
         */
        GIT_CHECKOUT_DONT_UPDATE_INDEX = (1 << 8),

        /** Don't refresh index/config/etc before doing checkout */
        GIT_CHECKOUT_NO_REFRESH = (1 << 9),

        /** Allow checkout to skip unmerged files */
        GIT_CHECKOUT_SKIP_UNMERGED = (1 << 10),
        /** For unmerged files, checkout stage 2 from index */
        GIT_CHECKOUT_USE_OURS = (1 << 11),
        /** For unmerged files, checkout stage 3 from index */
        GIT_CHECKOUT_USE_THEIRS = (1 << 12),

        /** Treat pathspec as simple list of exact match file paths */
        GIT_CHECKOUT_DISABLE_PATHSPEC_MATCH = (1 << 13),

        /** Ignore directories in use, they will be left empty */
        GIT_CHECKOUT_SKIP_LOCKED_DIRECTORIES = (1 << 18),

        /** Don't overwrite ignored files that exist in the checkout target */
        GIT_CHECKOUT_DONT_OVERWRITE_IGNORED = (1 << 19),

        /** Write normal merge files for conflicts */
        GIT_CHECKOUT_CONFLICT_STYLE_MERGE = (1 << 20),

        /** Include common ancestor data in diff3 format files for conflicts */
        GIT_CHECKOUT_CONFLICT_STYLE_DIFF3 = (1 << 21),

        /** Don't overwrite existing files or folders */
        GIT_CHECKOUT_DONT_REMOVE_EXISTING = (1 << 22),

        /** Normally checkout writes the index upon completion; this prevents that. */
        GIT_CHECKOUT_DONT_WRITE_INDEX = (1 << 23),

        /**
         * Perform a "dry run", reporting what _would_ be done but
         * without actually making changes in the working directory
         * or the index.
         */
        GIT_CHECKOUT_DRY_RUN = (1 << 24),

        /** Include common ancestor data in zdiff3 format for conflicts */
        GIT_CHECKOUT_CONFLICT_STYLE_ZDIFF3 = (1 << 25),

        /**
         * Do not do a checkout and do not fire callbacks; this is primarily
         * useful only for internal functions that will perform the
         * checkout themselves but need to pass checkout options into
         * another function, for example, `git_clone`.
        */
        GIT_CHECKOUT_NONE = (1 << 30),

        /*
         * THE FOLLOWING OPTIONS ARE NOT YET IMPLEMENTED
         */

        /** Recursively checkout submodules with same options (NOT IMPLEMENTED) */
        GIT_CHECKOUT_UPDATE_SUBMODULES = (1 << 16),
        /** Recursively checkout submodules if HEAD moved in super repo (NOT IMPLEMENTED) */
        GIT_CHECKOUT_UPDATE_SUBMODULES_IF_CHANGED = (1 << 17)
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int checkout_notify_cb(
        CheckoutNotifyFlags why,
        IntPtr path,
        IntPtr baseline,
        IntPtr target,
        IntPtr workdir,
        IntPtr payload);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void progress_cb(
            IntPtr strPtr,
            UIntPtr completed_steps,
            UIntPtr total_steps,
            IntPtr payload);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int perfdata_cb(
            IntPtr perfdata,
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

        public CheckoutNotifyFlags notify_flags;
        public checkout_notify_cb notify_cb;
        public IntPtr notify_payload;

        public progress_cb progress_cb;
        public IntPtr progress_payload;

        public GitStrArray paths;

        public IntPtr baseline;
        public IntPtr baseline_index;
        public IntPtr target_directory;

        public IntPtr ancestor_label;
        public IntPtr our_label;
        public IntPtr their_label;

        public perfdata_cb perfdata_cb;
        public IntPtr perfdata_payload;
    }

    /// <summary>
    /// An inteface for objects that specify parameters from which a
    /// GitCheckoutOpts struct can be populated.
    /// </summary>
    internal interface IConvertableToGitCheckoutOpts
    {
        CheckoutCallbacks GenerateCallbacks();

        CheckoutStrategy CheckoutStrategy { get; }

        CheckoutNotifyFlags CheckoutNotifyFlags { get; }
    }

    /// <summary>
    /// This wraps an IConvertableToGitCheckoutOpts object and can tweak the
    /// properties so that they are appropriate for a checkout performed as
    /// part of a FastForward merge. Most properties are passthrough to the
    /// wrapped object.
    /// </summary>
    internal class FastForwardCheckoutOptionsAdapter : IConvertableToGitCheckoutOpts
    {
        private IConvertableToGitCheckoutOpts internalOptions;

        internal FastForwardCheckoutOptionsAdapter(IConvertableToGitCheckoutOpts internalOptions)
        {
            this.internalOptions = internalOptions;
        }

        /// <summary>
        /// Passthrough to the wrapped object.
        /// </summary>
        /// <returns></returns>
        public CheckoutCallbacks GenerateCallbacks()
        {
            return internalOptions.GenerateCallbacks();
        }

        /// <summary>
        /// There should be no resolvable conflicts in a FastForward merge.
        /// Just perform checkout with the safe checkout strategy.
        /// </summary>
        public CheckoutStrategy CheckoutStrategy
        {
            get
            {
                return CheckoutStrategy.GIT_CHECKOUT_SAFE;
            }
        }

        /// <summary>
        /// Passthrough to the wrapped object.
        /// </summary>
        /// <returns></returns>
        public CheckoutNotifyFlags CheckoutNotifyFlags
        {
            get { return internalOptions.CheckoutNotifyFlags; }
        }
    }
}
