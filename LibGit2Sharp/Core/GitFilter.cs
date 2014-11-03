using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// A git filter
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal class GitFilter
    {
        public uint version;

        public IntPtr attributes;

        public git_filter_init_fn init;

        public git_filter_shutdown_fn shutdown;
        
        public IntPtr check;

        public IntPtr apply;

        public IntPtr cleanup;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        /// <summary>
        /// Initialize callback on filter
        /// 
        /// Specified as `filter.initialize`, this is an optional callback invoked
        /// before a filter is first used.  It will be called once at most.
        /// 
        /// If non-NULL, the filter's `initialize` callback will be invoked right
        /// before the first use of the filter, so you can defer expensive
        /// initialization operations (in case libgit2 is being used in a way that doesn't need the filter).
        /// </summary>
        public delegate int git_filter_init_fn(IntPtr filter);

        /// <summary>
        /// Shutdown callback on filter
        /// 
        /// Specified as `filter.shutdown`, this is an optional callback invoked
        /// when the filter is unregistered or when libgit2 is shutting down.  It
        /// will be called once at most and should release resources as needed.
        /// This may be called even if the `initialize` callback was not made.
        /// Typically this function will free the `git_filter` object itself.
        /// </summary>
        public delegate void git_filter_shutdown_fn(IntPtr filter);

    }
}