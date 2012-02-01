using System;

namespace LibGit2Sharp.Core
{
    internal class SignatureSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandle()
        {
            NativeMethods.git_signature_free(handle);
            return true;
        }
    }
}
