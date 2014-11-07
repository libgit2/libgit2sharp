using System;
using System.Runtime.InteropServices;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A filter
    /// </summary>
    public sealed class Filter
    {
        private readonly IntPtr nativeFilter;
        private readonly string filterName;
        private readonly string attributes;
        private readonly int version;
        private readonly FilterCallbacks filterCallbacks;
        private readonly GitFilter managedFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// And allocates the filter natively. 
        /// </summary>
        public Filter(string name, string attributes, int version, FilterCallbacks filterCallbacks)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(attributes, "attributes");
            Ensure.ArgumentNotNull(version, "version");
            Ensure.ArgumentNotNull(filterCallbacks, "filterCallbacks");

            this.filterName = name;
            this.attributes = attributes;
            this.version = version;
            this.filterCallbacks = filterCallbacks;

            managedFilter = new GitFilter
            {
                attributes = EncodingMarshaler.FromManaged(Encoding.UTF8, attributes),
                version = (uint) version,
                init = InitializeCallback,
                shutdown = ShutdownCallback,
                check = CheckCallback,
                apply = ApplyCallback,
                cleanup = CleanUpCallback
            };

            nativeFilter = Marshal.AllocHGlobal(Marshal.SizeOf(managedFilter));
            Marshal.StructureToPtr(managedFilter, nativeFilter, false);
        }

        internal Filter(string name, IntPtr filterPtr)
        {
            nativeFilter = filterPtr;
            managedFilter = nativeFilter.MarshalAs<GitFilter>();
            filterName = name;
            attributes = EncodingMarshaler.FromNative(Encoding.UTF8, managedFilter.attributes);
            version = (int) managedFilter.version;
        }

        /// <summary>
        /// The name that this filter was registered with
        /// </summary>
        public string Name
        {
            get { return filterName; }
        }

        /// <summary>
        /// The filter attributes.
        /// </summary>
        public string Attributes
        {
            get { return attributes; }
        }

        /// <summary>
        /// The version of the filter
        /// </summary>
        public int Version
        {
            get { return version; }
        }

        /// <summary>
        /// Register this filter
        /// </summary>
        public void Register()
        {
            Proxy.git_filter_register(filterName, nativeFilter, 1);
        }

        /// <summary>
        /// Remove the filter from the registry, and frees the native heap allocation.
        /// </summary>
        public void Deregister()
        {
            Proxy.git_filter_unregister(Name);
            Marshal.FreeHGlobal(nativeFilter);
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
        private int InitializeCallback(IntPtr filter)
        {
            return filterCallbacks.CustomInitializeCallback();
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
        private void ShutdownCallback(IntPtr gitFilter)
        {
            filterCallbacks.CustomShutdownCallback();
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
        private int CheckCallback(IntPtr gitFilter, IntPtr payload, GitFilterSource filterSource, IntPtr attributeValues)
        {
            return filterCallbacks.CustomCheckCallback();
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
        private int ApplyCallback(IntPtr gitFilter, IntPtr payload, GitBuf gitBufferTo, GitBuf gitBufferFrom, GitFilterSource filterSource)
        {
            return filterCallbacks.CustomApplyCallback();
        }

        /// <summary>
        /// Callback to clean up after filtering has been applied. Specified as `filter.cleanup`, this is an optional callback invoked
        /// after the filter has been applied.  If the `check` or `apply` callbacks allocated a `payload` 
        /// to keep per-source filter state, use this  callback to free that payload and release resources as required.
        /// </summary>
        private void CleanUpCallback(IntPtr gitFilter, IntPtr payload)
        {

        }
    }

    /// <summary>
    /// A filter registry
    /// </summary>
    public class FilterRegistry
    {
        /// <summary>
        /// Looks up a registered filter by its name. 
        /// </summary>
        /// <param name="name">The name to look up</param>
        /// <returns>The found matching filter</returns>
        public Filter LookupByName(string name)
        {
            return new Filter(name, Proxy.git_filter_lookup(name));
        }
    }

    /// <summary>
    /// The callbacks for a filter to execute
    /// </summary>
    public class FilterCallbacks
    {
        private readonly Func<int> customCheckCallback;
        private readonly Func<int> customApplyCallback;
        private readonly Func<int> customInitializeCallback;
        private readonly Action customShutdownCallback;

        private readonly Func<int> passThroughFunc = () => (int) GitErrorCode.PassThrough;
        private readonly Func<int> passThroughSuccess = () => 0;

        /// <summary>
        /// The callbacks for a filter to execute
        /// </summary>
        /// <param name="customCheckCallback">The check callback</param>
        /// <param name="customApplyCallback">the apply callback</param>
        /// <param name="customShutdownCallback">The shutdown callback</param>
        public FilterCallbacks(
            Func<int> customCheckCallback = null,
            Func<int> customApplyCallback = null, 
            Action customShutdownCallback = null)
        {
            this.customCheckCallback = customCheckCallback ?? passThroughFunc;
            this.customApplyCallback = customApplyCallback ?? passThroughFunc;
            this.customShutdownCallback = customShutdownCallback ??  (() => { });
            this.customInitializeCallback = customInitializeCallback ?? passThroughFunc;
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
        public Func<int> CustomCheckCallback
        {
            get
            {
                return customCheckCallback;
            }
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
        public Func<int> CustomApplyCallback
        {
            get
            {
                return customApplyCallback;
            }
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
        public Action CustomShutdownCallback
        {
            get
            {
                return customShutdownCallback;
            }
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
        public Func<int> CustomInitializeCallback
        {
            get
            {
                return passThroughSuccess;
            }
        }
    }
}