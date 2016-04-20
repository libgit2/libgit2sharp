using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// An object representing the registration of a Filter type with libgit2
    /// </summary>
    public sealed class FilterRegistration
    {
        /// <summary>
        /// Maximum priority value a filter can have. A value of 200 will be run last on checkout and first on checkin.
        /// </summary>
        public const int FilterPriorityMax = 200;
        /// <summary>
        /// Minimum priority value a filter can have. A value of 0 will be run first on checkout and last on checkin.
        /// </summary>
        public const int FilterPriorityMin = 0;

        /// <summary>
        ///
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="priority"></param>
        internal FilterRegistration(Filter filter, int priority)
        {
            System.Diagnostics.Debug.Assert(filter != null);
            System.Diagnostics.Debug.Assert(priority >= FilterPriorityMin && priority <= FilterPriorityMax);

            Filter = filter;
            Priority = priority;

            // marshal the git_filter strucutre into native memory
            FilterPointer = Marshal.AllocHGlobal(Marshal.SizeOf(filter.GitFilter));
            Marshal.StructureToPtr(filter.GitFilter, FilterPointer, false);

            // register the filter with the native libary
            Proxy.git_filter_register(filter.Name, FilterPointer, priority);
        }
        /// <summary>
        /// Finalizer called by the <see cref="GC"/>, deregisters and frees native memory associated with the registered filter in libgit2.
        /// </summary>
        ~FilterRegistration()
        {
            // deregister the filter
            GlobalSettings.DeregisterFilter(this);
            // clean up native allocations
            Free();
        }

        /// <summary>
        /// Gets if the registration and underlying filter are valid.
        /// </summary>
        public bool IsValid { get { return !freed; } }
        /// <summary>
        /// The registerd filters
        /// </summary>
        public readonly Filter Filter;
        /// <summary>
        /// The name of the filter in the libgit2 registry
        /// </summary>
        public string Name { get { return Filter.Name; } }
        /// <summary>
        /// The priority of the registered filter
        /// </summary>
        public readonly int Priority;

        private readonly IntPtr FilterPointer;

        private bool freed;

        internal void Free()
        {
            if (!freed)
            {
                // unregister the filter with the native libary
                Proxy.git_filter_unregister(Filter.Name);
                // release native memory
                Marshal.FreeHGlobal(FilterPointer);
                // remember to not do this twice
                freed = true;
            }
        }
    }
}
