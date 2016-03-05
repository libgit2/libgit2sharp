using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    /// Callback delegates for a <see cref="Repository"/> to invoke when appropriate. 
    /// Use <see cref="Repository.RegisterCallbacks(RepositoryCallbacks)"/> to register
    /// the association.
    /// </summary>
    public sealed class RepositoryCallbacks
    {
        /// <summary>
        /// Delegate called by the repository after a checkout has completed.
        /// </summary>
        public PostCheckoutDelegate PostCheckoutCallback;
        /// <summary>
        /// Delegate called by a repository after a commit has been created.
        /// </summary>
        public PostCommitDelegate PostCommitCallback;
        /// <summary>
        /// Delegate called by a repository before a push is completed, after negotiation is successful.
        /// </summary>
        public PrePushDelegate PrePushCallback;
    }

    /// <summary>
    /// Called by the repository after a checkout has completed.
    /// </summary>
    /// <param name="repository">The repository object which called the delegate.</param>
    /// <param name="oldHead">The previous head of the repository.</param>
    /// <param name="newHead">The current (new) head of the repository.</param>
    /// <param name="branchSwitch">True if a whole tree checkout occured; false otherwise.</param>
    public delegate void PostCheckoutDelegate(Repository repository, Reference oldHead, Reference newHead, bool branchSwitch);

    /// <summary>
    /// Called by a repository after a commit has been created.
    /// </summary>
    /// <param name="repository">The repository object which called the delegate.</param>
    public delegate void PostCommitDelegate(Repository repository);

    /// <summary>
    /// Called by a repository before a push is completed, after it has checked the remote status, 
    /// but before anything has been pushed.
    /// </summary>
    /// <param name="repository">The repository object which called the delegate.</param>
    /// <param name="remoteName">Name of the remote to which the push is being done; null if the name is unknown.</param>
    /// <param name="remoteUrl">URL to which the push is being done.</param>
    /// <param name="updates">The list of updates to be sent to the server as part of the push.</param>
    /// <returns>True is the push should continue; false otherwise.</returns>
    public delegate bool PrePushDelegate(Repository repository, string remoteName, string remoteUrl, IEnumerable<PushUpdate> updates);
}
