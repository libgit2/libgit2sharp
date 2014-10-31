using System;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A filter
    /// </summary>
    public sealed class Filter
    {
        private GitFilter filter;
        private readonly string name;
        private readonly string attributes;
        private readonly int version;

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// </summary>
        public Filter(string name, string attributes, int version)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(attributes, "attributes");
            Ensure.ArgumentNotNull(version, "version");

            this.name = name;
            this.attributes = attributes;
            this.version = version;
            filter = new GitFilter
            {
                attributes = EncodingMarshaler.FromManaged(Encoding.UTF8, attributes),
                version =  (uint)version
            };
        }

        internal Filter(string name, IntPtr filterPtr)
        {
            var handle = filterPtr.MarshalAs<GitFilter>();
            this.name = name;
            this.version = (int)handle.version;
            this.attributes = LaxUtf8Marshaler.FromNative(handle.attributes);
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
            filter = Proxy.git_filter_register(name, ref filter, 1);
        }

        /// <summary>
        /// Remove the filter from the registry.
        /// </summary>
        public void Deregister()
        {
            Proxy.git_filter_unregister(Name);
        }
    }

    /// <summary>
    /// A filter registry
    /// </summary>
    public static class FilterRegistry
    {
        /// <summary>
        /// Looks up a registered filter by its name. 
        /// </summary>
        /// <param name="name">The name to look up</param>
        /// <returns>The found matching filter</returns>
        public static Filter LookupByName(string name)
        {
            return new Filter(name, Proxy.git_filter_lookup(name));
        }
    }
}