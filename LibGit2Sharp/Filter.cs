using System;
using System.Runtime.InteropServices;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A filter
    /// </summary>
    public sealed class Filter
    {
        private GitFilter managedFilter;
        private IntPtr nativeFilter;

        private readonly string filterName;
        private readonly string attributes;
        private readonly int version;


        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// And allocates the filter natively. 
        /// </summary>
        public Filter(string name, string attributes, int version)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(attributes, "attributes");
            Ensure.ArgumentNotNull(version, "version");

            this.filterName = name;
            this.attributes = attributes;
            this.version = version;

            managedFilter = new GitFilter
            {
                attributes = EncodingMarshaler.FromManaged(Encoding.UTF8, attributes),
                version =  (uint)version,
                init =  FilterCallbacks.InitializeCallback,
                shutdown = FilterCallbacks.ShutdownCallback,
                check = FilterCallbacks.CheckCallback,
                apply = FilterCallbacks.ApplyCallback,
                cleanup = FilterCallbacks.CleanUpCallback
            };

            nativeFilter = Marshal.AllocHGlobal(Marshal.SizeOf(managedFilter));
            Marshal.StructureToPtr(managedFilter, nativeFilter, false);
        }

        internal Filter(string name, IntPtr filterPtr)
        {
            nativeFilter = filterPtr;
            managedFilter = nativeFilter.MarshalAs<GitFilter>();
            filterName = name;
            attributes = EncodingMarshaler.FromNative(Encoding.UTF8, this.managedFilter.attributes);
            version = (int) managedFilter.version;
        }

        /// <summary>
        /// The name that this filter was registered with
        /// </summary>
        public string Name
        {
            get { return filterName; }
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
           Proxy.git_filter_register(filterName, nativeFilter, 1);
        }

        /// <summary>
        /// Remove the filter from the registry, and frees the native heap allocation.
        /// </summary>
        public void Deregister()
        {
            Proxy.git_filter_unregister(Name);
            Marshal.FreeHGlobal(nativeFilter);
        }

        private static class FilterCallbacks
        {
            // Because our GitFilter structure exists on the managed heap only for a short time (to be marshaled
            // to native memory with StructureToPtr), we need to bind to static delegates. If at construction time
            // we were to bind to the methods directly, that's the same as newing up a fresh delegate every time.
            // Those delegates won't be rooted in the object graph and can be collected as soon as StructureToPtr finishes.

            public static readonly GitFilter.git_filter_init_fn InitializeCallback = Initialize;
            public static readonly GitFilter.git_filter_shutdown_fn ShutdownCallback = Shutdown;
            public static readonly GitFilter.git_filter_check_fn CheckCallback = Check;
            public static readonly GitFilter.git_filter_apply_fn ApplyCallback = Apply;
            public static readonly GitFilter.git_filter_cleanup_fn CleanUpCallback = CleanUp;

            private static int Initialize(IntPtr filter)
            {
                return 0;
            }

            private static void Shutdown(IntPtr gitFilter)
            {
            }

            private static int Check(IntPtr gitFilter, IntPtr payload, GitFilterSource filterSource, IntPtr attributeValues)
            {
                return 0;
            }

            private static int Apply(IntPtr gitFilter, IntPtr payload, IntPtr gitBufferTo, IntPtr gitBufferFrom, GitFilterSource filterSource)
            {
                return 0;
            }

            private static void CleanUp(IntPtr gitFilter, IntPtr payload)
            {

            }
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
            return new Filter(name, Proxy.git_filter_lookup(name));
        }
    }
}