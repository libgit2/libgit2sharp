using System;
namespace LibGit2Sharp.Core.Handles
{
    internal class IndexerSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_indexer_free(handle);
            return true;
        }
    }
}

