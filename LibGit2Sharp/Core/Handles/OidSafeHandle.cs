﻿using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class OidSafeHandle : NotOwnedSafeHandleBase
    {
        private GitOid MarshalAsGitOid()
        {
            return (GitOid)Marshal.PtrToStructure(handle, typeof(GitOid));
        }

        public ObjectId MarshalAsObjectId()
        {
            return new ObjectId(MarshalAsGitOid());
        }
    }
}
