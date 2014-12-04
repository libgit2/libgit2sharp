using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A filter
    /// </summary>
    public class Filter : IEquatable<Filter>
    {
        private static readonly LambdaEqualityHelper<Filter> equalityHelper =
        new LambdaEqualityHelper<Filter>(x => x.Name, x => x.Attributes);

        private readonly string name;
        private readonly string attributes;

        private readonly GitFilter managedFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// And allocates the filter natively. 
        /// </summary>
        protected Filter(string name, string attributes)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(attributes, "attributes");

            this.name = name;
            this.attributes = attributes;

            managedFilter = new GitFilter
            {
                attributes = GitFilter.GetAttributesFromManaged(attributes),
                init = InitializeCallback,
                apply = ApplyCallback,
                check = CheckCallback,
                shutdown = ShutdownCallback,
                cleanup = CleanUpCallback
            };
        }

        internal Filter(string name, GitFilterSafeHandle filterPtr)
        {
            this.name = name;
            managedFilter = filterPtr.MarshalFromNative();
            attributes = managedFilter.ManagedAttributes();
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
        /// The marshelled filter
        /// </summary>
        internal GitFilter ManagedFilter
        {
            get { return managedFilter; }
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
        protected virtual int Initialize()
        {
            return 0;
        }

        /// <summary>
        /// Decides if a given source needs to be filtered.
        /// </summary>
        /// <param name="attributes">The attributes that this filter was created for.</param>
        /// <param name="filterSource">The source of the filter</param>
        /// <returns>0 if successful and -30 to skip and pass through</returns>
        protected virtual int Check(string attributes, FilterSource filterSource)
        {
            return (int)GitErrorCode.PassThrough;
        }

        /// <summary>
        /// Clean the input stream and write to the output stream.
        /// </summary>
        /// <param name="input">The git buf input reader</param>
        /// <param name="output">The git buf output writer</param>
        /// <returns>0 if successful and -30 to skip and pass through</returns>
        protected virtual int Clean(GitBufReader input, GitBufWriter output)
        {
            return (int)GitErrorCode.PassThrough;
        }

        /// <summary>
        /// Smudge the input stream and write to the output stream.
        /// </summary>
        /// <param name="input">The git buf input reader</param>
        /// <param name="output">The git buf output writer</param>
        /// <returns>0 if successful and -30 to skip and pass through</returns>
        protected virtual int Smudge(GitBufReader input, GitBufWriter output)
        {
            return (int)GitErrorCode.PassThrough;
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
        protected virtual void ShutDown()
        {
        }

        /// <summary>
        /// Callback to clean up after filtering has been applied. Specified as `filter.cleanup`, this is an optional callback invoked
        /// after the filter has been applied.  If the `check` or `apply` callbacks allocated a `payload` 
        /// to keep per-source filter state, use this  callback to free that payload and release resources as required.
        /// </summary>
        protected virtual void CleanUp()
        {
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
        int InitializeCallback(IntPtr gitFilter)
        {
            return Initialize();
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
        int CheckCallback(GitFilter gitFilter, IntPtr payload, IntPtr filterSourcePtr, IntPtr attributeValues)
        {
            //string attributes = GitFilter.GetAttributesFromPointer(attributeValues);
            string attributes1 = GitFilter.GetAttributesFromPointer(gitFilter.attributes);
            var filterSource = FilterSource.FromNativePtr(filterSourcePtr);
            return Check(attributes1, filterSource);
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
        int ApplyCallback(GitFilter gitFilter, IntPtr payload, 
            IntPtr gitBufferToPtr, IntPtr gitBufferFromPtr, IntPtr filterSourcePtr)
        {
            var filterSource = FilterSource.FromNativePtr(filterSourcePtr);
            var reader = new GitBufReader(gitBufferFromPtr);
            var writer = new GitBufWriter(gitBufferToPtr);

            return filterSource.SourceMode == FilterMode.Clean ? Clean(reader, writer) : Smudge(reader, writer);
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
        void ShutdownCallback(IntPtr gitFilter)
        {
            ShutDown();
        }

        /// <summary>
        /// Callback to clean up after filtering has been applied. Specified as `filter.cleanup`, this is an optional callback invoked
        /// after the filter has been applied.  If the `check` or `apply` callbacks allocated a `payload` 
        /// to keep per-source filter state, use this  callback to free that payload and release resources as required.
        /// </summary>
        void CleanUpCallback(IntPtr gitFilter, IntPtr payload)
        {
            CleanUp();
        }
    }
}