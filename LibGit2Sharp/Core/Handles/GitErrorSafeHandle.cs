using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class GitErrorSafeHandle : NotOwnedSafeHandleBase
    {
        public GitError MarshalAsGitError()
        {
            // Required on Mono < 3.0.8
            // https://bugzilla.xamarin.com/show_bug.cgi?id=11417
            // https://github.com/mono/mono/commit/9cdddca7ec283f3b9181f3f69c1acecc0d9cc289
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            return (GitError)Marshal.PtrToStructure(handle, typeof(GitError));
        }
    }
}
