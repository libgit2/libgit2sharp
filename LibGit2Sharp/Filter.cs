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
            filter = new GitFilter {attributes = attributes, version = (uint) version};
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
}