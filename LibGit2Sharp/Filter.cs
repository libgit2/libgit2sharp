using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A filter
    /// </summary>
    public sealed class Filter : IDisposable
    {
        private GitFilter filter;
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// </summary>
        public Filter(string name, string attributes, int version)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(attributes, "attributes");
            Ensure.ArgumentNotNull(version, "version");

            this.name = name;

            filter = new GitFilter();
            filter.attributes = attributes;
            filter.version = (uint)version;
            filter = Proxy.git_filter_register(name, ref filter, 1);
        }

        /// <summary>
        /// The name that this filter was registered with
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        public void Dispose()
        {
            Proxy.git_filter_unregister(Name);
            //Proxy.git_filter_free(ref filter);
        }
    }
}