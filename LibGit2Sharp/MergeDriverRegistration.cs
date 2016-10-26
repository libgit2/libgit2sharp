using System;
using LibGit2Sharp.Core;
using System.Runtime.InteropServices;

namespace LibGit2Sharp
{
    /// <summary>
    /// An object representing the registration of a custom merge driver in libgit2
    /// </summary>
    public sealed class MergeDriverRegistration
    {
        /// <summary>
        /// Construct a registration object for the supplied merge driver
        /// </summary>
        /// <param name="driver">Driver to use</param>
        internal MergeDriverRegistration(MergeDriver driver)
        {
            System.Diagnostics.Debug.Assert(driver != null);
            MergeDriver = driver;

            // marshal the git_merge_driver structure into native memory
            MergeDriverPointer = Marshal.AllocHGlobal(Marshal.SizeOf(driver.GitMergeDriver));
            Marshal.StructureToPtr(driver.GitMergeDriver, MergeDriverPointer, false);

            // register the merge driver with the native libary
            Proxy.git_merge_driver_register(driver.Name, MergeDriverPointer);
        }

        /// <summary>
        /// Finalizer called by the <see cref="GC"/>, deregisters and frees native memory associated with the registered merge driver in libgit2.
        /// </summary>
        ~MergeDriverRegistration()
        {
            // Deregister the merge driver
            GlobalSettings.DeregisterMergeDriver(this);

            // Clean up native allocations
            Free();
        }

        /// <summary>
        /// Gets if the registration and underlying merge driver are valid.
        /// </summary>
        public bool IsValid { get { return !freed; } }

        /// <summary>
        /// The registerd merge drivers
        /// </summary>
        public readonly MergeDriver MergeDriver;

        /// <summary>
        /// The name of the driver in the libgit2 registry
        /// </summary>
        public string Name { get { return MergeDriver.Name; } }

        private readonly IntPtr MergeDriverPointer;

        private bool freed;

        internal void Free()
        {
            if (!freed)
            {
                // unregister the merge driver with the native libary
                Proxy.git_merge_driver_unregister(MergeDriver.Name);
                // release native memory
                Marshal.FreeHGlobal(MergeDriverPointer);
                // remember to not do this twice
                freed = true;
            }
        }
    }
}
