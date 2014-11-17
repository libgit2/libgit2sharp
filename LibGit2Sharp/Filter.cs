using System;
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
        private GitFilterSafeHandle nativeFilter;

        private GitFilter.git_filter_apply_fn applyCallback;
        private GitFilter.git_filter_check_fn checkCallback;
        private GitFilter.git_filter_init_fn initCallback;
        private GitFilter.git_filter_shutdown_fn shutdownCallback;
        private GitFilter.git_filter_cleanup_fn cleanCallback;

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
        }

        internal Filter(string name, GitFilterSafeHandle filterPtr)
        {
            this.name = name;
            nativeFilter = filterPtr;
            managedFilter = nativeFilter.MarshalFromNative();
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
            managedFilter = new GitFilter
            {
                attributes = EncodingMarshaler.FromManaged(Encoding.UTF8, attributes),
                version = (uint)version,
                init = initCallback,
                apply = applyCallback,
                check = checkCallback,
                shutdown = shutdownCallback,
                cleanup = cleanCallback
            };
            nativeFilter = new GitFilterSafeHandle(managedFilter);

            Proxy.git_filter_register(name, nativeFilter, 0);
        }

        /// <summary>
        /// Remove the filter from the registry, and frees the native heap allocation.
        /// </summary>
        public void Deregister()
        {
            Proxy.git_filter_unregister(name);
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
        private int CheckCallback(IntPtr gitFilter, IntPtr payload, IntPtr filterSource, IntPtr attributeValues)
        {
            return filterCallbacks.CustomCheckCallback(FilterSource.FromNativePtr(filterSource));
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
            return filterCallbacks.CustomApplyCallback(FilterSource.FromNativePtr(filterSource));
        }

        /// <summary>
        /// Callback to clean up after filtering has been applied. Specified as `filter.cleanup`, this is an optional callback invoked
        /// after the filter has been applied.  If the `check` or `apply` callbacks allocated a `payload` 
        /// to keep per-source filter state, use this  callback to free that payload and release resources as required.
        /// </summary>
        private void CleanUpCallback(IntPtr gitFilter, IntPtr payload)
        {
            filterCallbacks.CustomCleanUpCallback();
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
    /// These values control which direction of change is with which which a filter is being applied.
    /// </summary>
    public enum FilterMode
    {
        /// <summary>
        /// Smudge - occurs when exporting a file from the Git object database to the working directory,
        /// </summary>
        Smudge = 0,

        /// <summary>
        /// Clean - occurs when importing a file from the working directory to the Git object database.
        /// </summary>
        Clean = (1 << 0),
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
        public virtual Filter LookupByName(string name)
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
        private readonly Func<FilterSource, int> customCheckCallback;
        private readonly Func<FilterSource, int> customApplyCallback;
        private readonly Func<int> customInitializeCallback;
        private readonly Action customShutdownCallback;
        private readonly Action customCleanUpCallback;

        private readonly Func<FilterSource, int> passThroughFunc = (source) => (int)GitErrorCode.PassThrough;
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
            Func<FilterSource, int> customCheckCallback = null,
            Func<FilterSource, int> customApplyCallback = null, 
            Action customShutdownCallback = null,
            Func<int> customInitializeCallback = null,
            Action customCleanupCallback = null)
        {
            this.customCheckCallback = customCheckCallback ?? passThroughFunc;
            this.customApplyCallback = customApplyCallback ?? passThroughFunc;
            this.customShutdownCallback = customShutdownCallback ??  (() => { });
            this.customInitializeCallback = customInitializeCallback ?? passThroughSuccess;
            this.customCleanUpCallback = customCleanupCallback ?? (() => { });
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
        public virtual Func<FilterSource, int> CustomCheckCallback
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
        public virtual Func<FilterSource, int> CustomApplyCallback
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
        public virtual Action CustomShutdownCallback
        {
            get
            {
                return customShutdownCallback;
            }
        }
        /// <summary>
        /// Callback to clean up after filtering has been applied. Specified as `filter.cleanup`, this is an optional callback invoked
        /// after the filter has been applied.  If the `check` or `apply` callbacks allocated a `payload` 
        /// to keep per-source filter state, use this  callback to free that payload and release resources as required.
        /// </summary>
        public virtual Action CustomCleanUpCallback
        {
            get
            {
                return customCleanUpCallback;
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
        public virtual Func<int> CustomInitializeCallback
        {
            get
            {
                return customInitializeCallback;
            }
        }
    }
}