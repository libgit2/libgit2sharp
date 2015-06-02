namespace LibGit2Sharp.Core.Handles
{
    internal class TransactionSafeHandle : SafeHandleBase
    {
        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_transaction_free(handle);
            return true;
        }
    }
}
