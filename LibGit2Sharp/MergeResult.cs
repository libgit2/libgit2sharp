using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    public class MergeResult
    {
        protected MergeResult(){}
        internal MergeResult(GitMergeResultHandle handle)
        {
            IsUpToDate = Proxy.git_merge_result_is_uptodate(handle);
            IsFastForward = Proxy.git_merge_result_is_fastforward(handle);

            if (IsFastForward)
            {
                FastForwardOid = Proxy.git_merge_result_fastforward_oid(handle);
            }
        }

        public virtual bool IsUpToDate { get; private set; }

        public virtual bool IsFastForward { get; private set; }

        internal GitOid FastForwardOid { get; private set; }
    }
}
