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
        public uint version = 1;

        public IntPtr attributes;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public git_filter_init_fn init;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public git_filter_shutdown_fn shutdown;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public git_filter_check_fn check;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public git_filter_apply_fn apply;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public git_filter_stream_fn stream;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public git_filter_cleanup_fn cleanup;

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
        /// Typically this function will free the `git_filter` object itself.
        /// </summary>
        public delegate void git_filter_shutdown_fn(IntPtr filter);

        /// <summary>
        /// Callback to decide if a given source needs this filter
        /// Specified as `filter.check`, this is an optional callback that checks if filtering is needed for a given source.
        ///
        /// It should return 0 if the filter should be applied (i.e. success), GIT_PASSTHROUGH if the filter should
        /// not be applied, or an error code to fail out of the filter processing pipeline and return to the caller.
        ///
        /// The `attr_values` will be set to the values of any attributes given in the filter definition.  See `git_filter` below for more detail.
        ///
        /// The `payload` will be a pointer to a reference payload for the filter. This will start as NULL, but `check` can assign to this
        /// pointer for later use by the `apply` callback.  Note that the value should be heap allocated (not stack), so that it doesn't go
        /// away before the `apply` callback can use it.  If a filter allocates and assigns a value to the `payload`, it will need a `cleanup`
        /// callback to free the payload.
        /// </summary>
        public delegate int git_filter_check_fn(
            GitFilter gitFilter,
            IntPtr payload,
            IntPtr filterSource,
            IntPtr attributeValues);

        /// <summary>
        /// Callback to actually perform the data filtering
        ///
        /// Specified as `filter.apply`, this is the callback that actually filters data.
        /// If it successfully writes the output, it should return 0.  Like `check`,
        /// it can return GIT_PASSTHROUGH to indicate that the filter doesn't want to run.
        /// Other error codes will stop filter processing and return to the caller.
        ///
        /// The `payload` value will refer to any payload that was set by the `check` callback.  It may be read from or written to as needed.
        /// </summary>
        public delegate int git_filter_apply_fn(
            GitFilter gitFilter,
            IntPtr payload,
            IntPtr gitBufTo,
            IntPtr gitBufFrom,
            IntPtr filterSource);

        public delegate int git_filter_stream_fn(
            out IntPtr git_writestream_out,
            GitFilter self,
            IntPtr payload,
            IntPtr filterSource,
            IntPtr git_writestream_next);

        /// <summary>
        /// Callback to clean up after filtering has been applied. Specified as `filter.cleanup`, this is an optional callback invoked
        /// after the filter has been applied.  If the `check` or `apply` callbacks allocated a `payload`
        /// to keep per-source filter state, use this  callback to free that payload and release resources as required.
        /// </summary>
        public delegate void git_filter_cleanup_fn(IntPtr gitFilter, IntPtr payload);
    }
    /// <summary>
    /// The file source being filtered
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal class GitFilterSource
    {
        public IntPtr repository;

        public IntPtr path;

        public GitOid oid;
    }
}
