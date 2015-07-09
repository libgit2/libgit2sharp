using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// A git_oidarray where the id array and ids themselves were allocated
    /// with libgit2's allocator. Only libgit2 can free this git_oidarray.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal class GitOidArrayNative : IDisposable
    {
        public GitOidArray Array;

        /// <summary>
        /// Reads each GitOid from the array.
        /// </summary>
        public GitOid[] ReadOids()
        {
            var count = checked((int)Array.Length.ToUInt32());

            GitOid[] toReturn = new GitOid[count];

            for (int i = 0; i < count; i++)
            {
                toReturn[i] = (Array.Ids + i * Marshal.SizeOf(typeof(GitOid))).MarshalAs<GitOid>();
            }

            return toReturn;
        }

        public void Dispose()
        {
            if (Array.Ids != IntPtr.Zero)
            {
                NativeMethods.git_oidarray_free(ref Array);
            }

            // Now that we've freed the memory, zero out the structure.
            Array.Reset();
        }
    }
}
