using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class OidSafeHandle : NotOwnedSafeHandleBase
    {
        private GitOid? MarshalAsGitOid()
        {
            return IsZero || IsInvalid ? null : (GitOid?)MarshalAsGitOid(handle);
        }

        private static GitOid MarshalAsGitOid(IntPtr data)
        {
            var gitOid = new GitOid { Id = new byte[GitOid.Size] };
            Marshal.Copy(data, gitOid.Id, 0, GitOid.Size);
            return gitOid;
        }

        public ObjectId MarshalAsObjectId()
        {
            return MarshalAsGitOid();
        }
    }
}
