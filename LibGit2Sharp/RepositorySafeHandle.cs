using LibGit2Sharp.Core;
using Microsoft.Win32.SafeHandles;

namespace LibGit2Sharp
{
    public class RepositorySafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public RepositorySafeHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            NativeMethods.git_repository_free(handle);
            return true;
        }
    }
}