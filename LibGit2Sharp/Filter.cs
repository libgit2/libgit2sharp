using System;
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
        new LambdaEqualityHelper<Filter>(x => x.Name, x => x.Attributes);

        private readonly string name;
        private readonly string attributes;
        private readonly FilterCallbacks filterCallbacks;

        private GitFilter managedFilter;
        private GitFilterSafeHandle nativeFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// And allocates the filter natively. 
        /// </summary>
        public Filter(string name, string attributes, FilterCallbacks filterCallbacks)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(attributes, "attributes");
            Ensure.ArgumentNotNull(filterCallbacks, "filterCallbacks");

            this.name = name;
            this.attributes = attributes;
            this.filterCallbacks = filterCallbacks;
        }

        internal Filter(string name, GitFilterSafeHandle filterPtr)
        {
            this.name = name;
            nativeFilter = filterPtr;
            managedFilter = nativeFilter.MarshalFromNative();
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
        /// Register this filter
        /// </summary>
        public void Register()
        {
            managedFilter = new GitFilter
            {
                attributes = GitFilter.GetAttributesFromManaged(attributes),
                init = filterCallbacks.InitializeCallback,
                apply = filterCallbacks.ApplyCallback,
                check = filterCallbacks.CheckCallback,
                shutdown = filterCallbacks.ShutdownCallback,
                cleanup = filterCallbacks.CleanUpCallback
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
}