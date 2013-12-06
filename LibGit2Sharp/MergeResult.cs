using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using System.Diagnostics;

namespace LibGit2Sharp
{
    public class MergeResult
    {
        public MergeResult(){}
        internal MergeResult(GitMergeResultHandle handle)
        {
            _isUpToDate = Proxy.git_merge_result_is_uptodate(handle);
            _isFastForward = Proxy.git_merge_result_is_fastforward(handle);

            if (_isFastForward)
                _oid = Proxy.git_merge_result_fastforward_oid(handle);
        }

        private bool _isUpToDate;
        public virtual bool IsUpToDate
        {
            get { return _isUpToDate; }
        }

        private bool _isFastForward;
        public virtual bool IsFastForward
        {
            get { return _isFastForward; }
        }

        private readonly GitOid _oid;
        internal GitOid FastForwardOid
        {
            get { return _oid; }
        }
    }
}
