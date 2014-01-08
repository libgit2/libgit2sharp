using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class OidSafeHandle : NotOwnedSafeHandleBase
    {
        private GitOid? MarshalAsGitOid()
        {
            return IsInvalid ? null : (GitOid?)handle.MarshalAs<GitOid>();
        }

        public ObjectId MarshalAsObjectId()
        {
            return MarshalAsGitOid();
        }
    }
}
