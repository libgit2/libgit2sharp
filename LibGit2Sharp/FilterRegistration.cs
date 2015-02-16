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
        internal FilterRegistration(Filter filter)
        {
            Ensure.ArgumentNotNull(filter, "filter");
            Name = filter.Name;

            FilterPointer = Marshal.AllocHGlobal(Marshal.SizeOf(filter.GitFilter));
            Marshal.StructureToPtr(filter.GitFilter, FilterPointer, false);
        }

        /// <summary>
        /// The name of the filter in the libgit2 registry
        /// </summary>
        public string Name { get; private set; }

        internal IntPtr FilterPointer { get; private set; }

        internal void Free()
        {
            Marshal.FreeHGlobal(FilterPointer);
            FilterPointer = IntPtr.Zero;
        }
    }
}
