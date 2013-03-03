using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class OidSafeHandle : NotOwnedSafeHandleBase
    {
        private GitOid? MarshalAsGitOid()
        {
            return IsInvalid ? null : (GitOid?)Marshal.PtrToStructure(handle, typeof(GitOid));
        }

        public ObjectId MarshalAsObjectId()
        {
            return MarshalAsGitOid();
        }
    }
}
