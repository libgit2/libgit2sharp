using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A filter
    /// </summary>
    public abstract class Filter : IEquatable<Filter>
    {
        private static readonly LambdaEqualityHelper<Filter> equalityHelper =
        new LambdaEqualityHelper<Filter>(x => x.Name, x => x.Attributes);

        private readonly string name;
        private readonly string attributes;

        private readonly GitFilter managedFilter;


        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// And allocates the filter natively.
        /// <param name="name">The unique name with which this filtered is registered with</param>
        /// <param name="attributes">A list of filterForAttributes which this filter applies to</param>
        /// </summary>
        protected Filter(string name, IEnumerable<string> attributes)
            : this(name, string.Join(",", attributes))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// And allocates the filter natively.
        /// <param name="name">The unique name with which this filtered is registered with</param>
        /// <param name="attributes">Either a single attribute, or a comma separated list of filterForAttributes for which this filter applies to</param>
        /// </summary>
        private Filter(string name, string attributes)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyEnumerable(attributes, "attributes");

            this.name = name;
            this.attributes = attributes;

            managedFilter = new GitFilter
            {
                attributes = EncodingMarshaler.FromManaged(Encoding.UTF8, attributes),
                init = InitializeCallback,
                apply = ApplyCallback,
                check = CheckCallback
            };
        }

        /// <summary>
        /// The name that this filter was registered with
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// The filter filterForAttributes.
        /// </summary>
        public IEnumerable<string> Attributes
        {
            get { return attributes.Split(','); }
        }

        /// <summary>
        /// The marshalled filter
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
        /// initialization operations (in case the library is being used in a way
        /// that doesn't need the filter.
        /// </summary>
        protected virtual int Initialize()
        {
            return 0;
        }

        /// <summary>
        /// Decides if a given source needs to be filtered by checking if the filter
        /// matches the current file extension.
        /// </summary>
        /// <param name="filterForAttributes">The filterForAttributes that this filter was created for.</param>
        /// <param name="filterSource">The source of the filter</param>
        /// <returns>0 if successful and -30 to skip and pass through</returns>
        protected virtual int Check(IEnumerable<string> filterForAttributes, FilterSource filterSource)
        {
            var fileInfo = new FileInfo(filterSource.Path);
            var matches = filterForAttributes.Any(currentExtension => string.Equals(fileInfo.Extension, currentExtension, StringComparison.Ordinal));
            return matches ? 0 : (int)GitErrorCode.PassThrough;
        }

        /// <summary>
        /// Clean the input stream and write to the output stream.
        /// </summary>
        /// <param name="path">The path of the file being filtered</param>
        /// <param name="input">The git buf input reader</param>
        /// <param name="output">The git buf output writer</param>
        /// <returns>0 if successful and -30 to skip and pass through</returns>
        protected virtual int Clean(string path, Stream input, Stream output)
        {
            return (int)GitErrorCode.PassThrough;
        }

        /// <summary>
        /// Smudge the input stream and write to the output stream.
        /// </summary>
        /// <param name="path">The path of the file being filtered</param>
        /// <param name="input">The git buf input reader</param>
        /// <param name="output">The git buf output writer</param>
        /// <returns>0 if successful and -30 to skip and pass through</returns>
        protected virtual int Smudge(string path, Stream input, Stream output)
        {
            return (int)GitErrorCode.PassThrough;
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
        /// The `attr_values` will be set to the values of any filterForAttributes given in the filter definition.  See `git_filter` below for more detail.
        ///
        /// The `payload` will be a pointer to a reference payload for the filter. This will start as NULL, but `check` can assign to this
        /// pointer for later use by the `apply` callback.  Note that the value should be heap allocated (not stack), so that it doesn't go
        /// away before the `apply` callback can use it.  If a filter allocates and assigns a value to the `payload`, it will need a `cleanup`
        /// callback to free the payload.
        /// </summary>
        /// <returns></returns>
        int CheckCallback(GitFilter gitFilter, IntPtr payload, IntPtr filterSourcePtr, IntPtr attributeValues)
        {
            string filterForAttributes = EncodingMarshaler.FromNative(Encoding.UTF8, gitFilter.attributes);
            var filterSource = FilterSource.FromNativePtr(filterSourcePtr);
            return Check(filterForAttributes.Split(','), filterSource);
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
            using (var reader = new GitBufReadStream(gitBufferFromPtr))
            using (var writer = new GitBufWriteStream(gitBufferToPtr))
            {
                return filterSource.SourceMode == FilterMode.Clean ?
                    Clean(filterSource.Path, reader, writer) :
                    Smudge(filterSource.Path, reader, writer);
            }
        }
    }
}
