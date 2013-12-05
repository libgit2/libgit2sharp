using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    internal class GitMergeResult
    {
        internal GitMergeResult(GitMergeResultHandle handle)
        {
            IsUpToDate = Proxy.git_merge_result_is_uptodate(handle);
            IsFastForward = Proxy.git_merge_result_is_fastforward(handle);

            if (IsFastForward)
            {
                FastForwardId = Proxy.git_merge_result_fastforward_oid(handle);
            }
        }

        public virtual bool IsUpToDate { get; private set; }

        public virtual bool IsFastForward { get; private set; }

        /// <summary>
        /// The ID that a fast-forward merge should advance to.
        /// </summary>
        public virtual ObjectId FastForwardId { get; private set; }

        public virtual MergeStatus Status
        {
            get
            {
                if (IsUpToDate)
                {
                    return MergeStatus.UpToDate;
                }
                else if (IsFastForward)
                {
                    return MergeStatus.FastForward;
                }
                else
                {
                    return MergeStatus.NonFastForward;
                }
            }
        }
    }
}
