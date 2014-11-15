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
    public sealed class Filter : IEquatable<Filter>
    {
        private static readonly LambdaEqualityHelper<Filter> equalityHelper =
        new LambdaEqualityHelper<Filter>(x => x.Name, x => x.Attributes,x => x.Version);

        private readonly string name;
        private readonly string attributes;
        private readonly int version;
        private readonly FilterCallbacks filterCallbacks;

        private GitFilter managedFilter;
        private GCHandle filterHandle;
        private readonly GitFilterSafeHandle nativeFilter;

        private GitFilter.git_filter_apply_fn applyCallback;
        private GitFilter.git_filter_check_fn checkCallback;
        private GitFilter.git_filter_init_fn initCallback;
        private GitFilter.git_filter_shutdown_fn shutdownCallback;
        private GitFilter.git_filter_cleanup_fn cleanCallback;

        private IntPtr applyCallbackHandle;
        private IntPtr checkCallbackHandle;
        private IntPtr initCallbackHandle;
        private GCHandle checkCallbackGCHandle;
        private GCHandle applyCallbackGCHandle;
        private GCHandle initCallbackGCHandle;
        private IntPtr shutdownCallbackHandle;
        private IntPtr cleanCallbackHandle;
        private GCHandle shutdownCallbackGCHandle;
        private GCHandle cleanCallbackGCHandle;

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

            this.name = name;
            this.attributes = attributes;
            this.version = version;
            this.filterCallbacks = filterCallbacks;

            applyCallback = ApplyCallback;
            checkCallback = CheckCallback;
            initCallback = InitializeCallback;
            shutdownCallback = ShutdownCallback;
            cleanCallback = CleanUpCallback;

            checkCallbackHandle = Marshal.GetFunctionPointerForDelegate(checkCallback);
            applyCallbackHandle = Marshal.GetFunctionPointerForDelegate(applyCallback);
            initCallbackHandle = Marshal.GetFunctionPointerForDelegate(initCallback);
            shutdownCallbackHandle = Marshal.GetFunctionPointerForDelegate(shutdownCallback);
            cleanCallbackHandle = Marshal.GetFunctionPointerForDelegate(cleanCallback);

            checkCallbackGCHandle = GCHandle.Alloc(checkCallbackHandle, GCHandleType.Pinned);
            applyCallbackGCHandle = GCHandle.Alloc(applyCallbackHandle, GCHandleType.Pinned);
            initCallbackGCHandle = GCHandle.Alloc(initCallbackHandle, GCHandleType.Pinned);
            shutdownCallbackGCHandle = GCHandle.Alloc(shutdownCallbackHandle, GCHandleType.Pinned);
            cleanCallbackGCHandle = GCHandle.Alloc(cleanCallbackHandle, GCHandleType.Pinned);

            managedFilter = new GitFilter
            {
                attributes = EncodingMarshaler.FromManaged(Encoding.UTF8, attributes),
                version = (uint)version,
                init = initCallbackHandle,
                apply = applyCallbackHandle,
                check = checkCallbackHandle,
                shutdown = shutdownCallbackHandle,
                cleanup = cleanCallbackHandle
            };

            filterHandle = GCHandle.Alloc(managedFilter, GCHandleType.Pinned);
            nativeFilter = new GitFilterSafeHandle(managedFilter);
        }

        internal Filter(string name, GitFilterSafeHandle filterPtr)
        {
            nativeFilter = filterPtr;
            Console.WriteLine(nativeFilter != null);
            managedFilter = nativeFilter.MarshalFromNative();
            this.name = name;
            attributes = EncodingMarshaler.FromNative(Encoding.UTF8, managedFilter.attributes);
            version = (int) managedFilter.version;
        }

        /// <summary>
        /// The name that this filter was registered with
        /// </summary>
        public string Name
        {
            get { return name; }
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
            Proxy.git_filter_register(name, nativeFilter, 0);
        }

        /// <summary>
        /// Remove the filter from the registry, and frees the native heap allocation.
        /// </summary>
        public void Deregister()
        {
            Proxy.git_filter_unregister(name);

            if (filterHandle.IsAllocated)
            {
                filterHandle.Free();
            }

            if (applyCallbackGCHandle.IsAllocated)
            {
                applyCallbackGCHandle.Free();
            }

            if (checkCallbackGCHandle.IsAllocated)
            {
                checkCallbackGCHandle.Free();
            }

            if (initCallbackGCHandle.IsAllocated)
            {
                initCallbackGCHandle.Free();
            }

            if (shutdownCallbackGCHandle.IsAllocated)
            {
                shutdownCallbackGCHandle.Free();
            }

            if (cleanCallbackGCHandle.IsAllocated)
            {
                cleanCallbackGCHandle.Free();
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
        private int InitializeCallback(IntPtr filter)
        {
            Console.WriteLine("Init");
            return 0;
           // return filterCallbacks.CustomInitializeCallback();
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
            Console.WriteLine("ShutDown");
           // filterCallbacks.CustomShutdownCallback();
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
        private int CheckCallback(IntPtr gitFilter, IntPtr payload, IntPtr filterSource, IntPtr attributeValues)
        {
            Console.WriteLine("Check");
            return 0;
            //return filterCallbacks.CustomCheckCallback();
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
        private int ApplyCallback(IntPtr gitFilter, IntPtr payload, IntPtr gitBufferTo, IntPtr gitBufferFrom, IntPtr filterSource)
        {
            Console.WriteLine("Apply");
            return 0;
            //return filterCallbacks.CustomApplyCallback();
        }

        /// <summary>
        /// Callback to clean up after filtering has been applied. Specified as `filter.cleanup`, this is an optional callback invoked
        /// after the filter has been applied.  If the `check` or `apply` callbacks allocated a `payload` 
        /// to keep per-source filter state, use this  callback to free that payload and release resources as required.
        /// </summary>
        private void CleanUpCallback(IntPtr gitFilter, IntPtr payload)
        {
            Console.WriteLine("Cleanup");
        }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Filter"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Filter"/>.</param>
        /// <returns>True if the specified <see cref="Object"/> is equal to the current <see cref="Filter"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Filter);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Filter"/> is equal to the current <see cref="Filter"/>.
        /// </summary>
        /// <param name="other">The <see cref="Filter"/> to compare with the current <see cref="Filter"/>.</param>
        /// <returns>True if the specified <see cref="Filter"/> is equal to the current <see cref="Filter"/>; otherwise, false.</returns>
        public bool Equals(Filter other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        /// Tests if two <see cref="Filter"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="Filter"/> to compare.</param>
        /// <param name="right">Second <see cref="Filter"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Filter left, Filter right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="Filter"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="Filter"/> to compare.</param>
        /// <param name="right">Second <see cref="Filter"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Filter left, Filter right)
        {
            return !Equals(left, right);
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
            GitFilterSafeHandle gitFilterLookup = Proxy.git_filter_lookup(name);
            return new Filter(name, gitFilterLookup);
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
                return passThroughFunc;
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