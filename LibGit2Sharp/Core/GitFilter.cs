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

        public IntPtr init;

        public git_filter_shutdown_fn shutdown;
        
        public IntPtr check;

        public IntPtr apply;

        public IntPtr cleanup;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        /// <summary>
        ///  Shutdown callback on filter
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