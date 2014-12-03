using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// The callbacks for a filter to execute
    /// </summary>
    public class FilterCallbacks
    {
        private readonly Func<FilterSource, string, int> customCheckCallback;
        private readonly Func<FilterSource, GitBufReader, GitBufWriter, int> customApplyCallback;
        private readonly Func<int> customInitializeCallback;
        private readonly Action customShutdownCallback;
        private readonly Action customCleanUpCallback;

        private readonly Func<int> passThroughSuccess = () => 0;

        /// <summary>
        /// Needed for mocking purposes
        /// </summary>
        protected FilterCallbacks()
        {
            
        }

        /// <summary>
        /// The callbacks for a filter to execute
        /// </summary>
        /// <param name="customCheckCallback">The check callback</param>
        /// <param name="customApplyCallback">the apply callback</param>
        /// <param name="customShutdownCallback">The shutdown callback</param>
        /// <param name="customInitializeCallback">The init callback</param>
        /// <param name="customCleanupCallback">The clean callback</param>
        public FilterCallbacks(
            Func<FilterSource, string, int> customCheckCallback = null,
            Func<FilterSource, GitBufReader, GitBufWriter, int> customApplyCallback = null, 
            Action customShutdownCallback = null,
            Func<int> customInitializeCallback = null,
            Action customCleanupCallback = null)
        {
            this.customCheckCallback = customCheckCallback ?? ((source, attr) => (int)GitErrorCode.PassThrough);
            this.customApplyCallback = customApplyCallback ?? ((source, reader, writer) => (int)GitErrorCode.PassThrough);
            this.customShutdownCallback = customShutdownCallback ??  (() => { });
            this.customInitializeCallback = customInitializeCallback ?? passThroughSuccess;
            this.customCleanUpCallback = customCleanupCallback ?? (() => { });
        }


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
        internal int InitializeCallback(IntPtr filter)
        {
            return customInitializeCallback();
        }

        /// <summary>
        /// Shutdown callback on filter
        /// 
        /// Specified as `filter.shutdown`, this is an optional callback invoked
        /// when the filter is unregistered or when libgit2 is shutting down.  It
        /// will be called once at most and should release resources as needed.
        /// This may be called even if the `initialize` callback was not made.
        /// Typically this function will free the `git_filter` object itself.
        /// </summary>
        internal void ShutdownCallback(IntPtr gitFilter)
        {
            customShutdownCallback();
        }

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
        /// <returns></returns>
        internal int CheckCallback(GitFilter gitFilter, IntPtr payload, IntPtr filterSourcePtr, IntPtr attributeValues)
        {
            //string attributes = GitFilter.GetAttributesFromPointer(attributeValues);
            string attributes1 = GitFilter.GetAttributesFromPointer(gitFilter.attributes);
            var filterSource = FilterSource.FromNativePtr(filterSourcePtr);
            return customCheckCallback(filterSource, attributes1);
        }

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
        internal int ApplyCallback(GitFilter gitFilter, IntPtr payload, IntPtr gitBufferToPtr, IntPtr gitBufferFromPtr,
             IntPtr filterSourcePtr)
        {
            var filterSource = FilterSource.FromNativePtr(filterSourcePtr);
            var reader = new GitBufReader(gitBufferFromPtr);
            var writer = new GitBufWriter(gitBufferToPtr);

            return customApplyCallback(filterSource, reader, writer);
        }

        /// <summary>
        /// Callback to clean up after filtering has been applied. Specified as `filter.cleanup`, this is an optional callback invoked
        /// after the filter has been applied.  If the `check` or `apply` callbacks allocated a `payload` 
        /// to keep per-source filter state, use this  callback to free that payload and release resources as required.
        /// </summary>
        internal void CleanUpCallback(IntPtr gitFilter, IntPtr payload)
        {
            customCleanUpCallback();
        }
    }
}