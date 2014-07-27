using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp.Core.Handles
{
    internal class StatusListSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_status_list_free(handle);
            return true;
        }
    }
}
