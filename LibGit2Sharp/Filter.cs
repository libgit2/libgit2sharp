using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A filter is a way to execute code against a file as it moves to and from the git
    /// repository and into the working directory. 
    /// </summary>
    public abstract class Filter : IEquatable<Filter>
    {
        private static readonly LambdaEqualityHelper<Filter> equalityHelper =
        new LambdaEqualityHelper<Filter>(x => x.Name, x => x.Attributes);

        private readonly string name;
        private readonly IEnumerable<FilterAttributeEntry> attributes;

        private readonly GitFilter gitFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// And allocates the filter natively.
        /// <param name="name">The unique name with which this filtered is registered with</param>
        /// <param name="attributes">A list of attributes which this filter applies to</param>
        /// </summary>
        protected Filter(string name, IEnumerable<FilterAttributeEntry> attributes)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(attributes, "attributes");

            this.name = name;
            this.attributes = attributes;
            var attributesAsString = string.Join(",", this.attributes.Select(attr => attr.FilterDefinition));

            gitFilter = new GitFilter
            {
                attributes = EncodingMarshaler.FromManaged(Encoding.UTF8, attributesAsString),
                init = InitializeCallback,
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
        public IEnumerable<FilterAttributeEntry> Attributes
        {
            get { return attributes; }
        }

        /// <summary>
        /// The marshalled filter
        /// </summary>
        internal GitFilter GitFilter
        {
            get { return gitFilter; }
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
        /// Clean the input stream and write to the output stream.
        /// </summary>
        /// <param name="path">The path of the file being filtered</param>
        /// <param name="input">The git buf input reader</param>
        /// <param name="output">The git buf output writer</param>
        /// <returns>0 if successful and <see cref="GitErrorCode.PassThrough"/> to skip and pass through</returns>
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
        /// <returns>0 if successful and <see cref="GitErrorCode.PassThrough"/> to skip and pass through</returns>
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
        int InitializeCallback(IntPtr filterPointer)
        {
            return Initialize();
        }
    }
}
