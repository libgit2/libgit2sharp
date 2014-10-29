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
        public Filter(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            this.name = name;
            try
            {
                filter = Proxy.git_filter_register(name, 1);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                //clean up
                Dispose();
            }
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
        }
    }
}