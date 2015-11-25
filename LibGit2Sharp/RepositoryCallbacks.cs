using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RepositoryCallbacks
    {
        public PostCheckoutDelegate PostCheckoutCallback;
        public PostCommitDelegate PostCommitCallback;
        public PrePushDelegate PrePushCallback;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="oldHead"></param>
    /// <param name="newHead"></param>
    /// <param name="branchSwitch"></param>
    public delegate void PostCheckoutDelegate(Repository repository, Reference oldHead, Reference newHead, bool branchSwitch);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    public delegate void PostCommitDelegate(Repository repository);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repository"></param>
    /// <param name="updates"></param>
    /// <returns></returns>
    public delegate bool PrePushDelegate(Repository repository, IEnumerable<PushUpdate> updates);
}
