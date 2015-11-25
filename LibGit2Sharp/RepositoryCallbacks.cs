using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    /// Set of callbacks analogous to Git hooks
    /// </summary>
    public sealed class RepositoryCallbacks
    {
        /// <summary>
        /// Gets registered with a <see cref="Repository"/> and called after checkout completes.
        /// </summary>
        public PostCheckoutDelegate PostCheckoutCallback;
        /// <summary>
        /// Gets registered with a <see cref="Repository"/> and called after a new commit is created.
        /// </summary>
        public PostCommitDelegate PostCommitCallback;
        /// <summary>
        /// Gets registered with a <see cref="Repository"/> and is called before Remote.Network.Push begins.
        /// </summary>
        public PrePushDelegate PrePushCallback;
    }

    /// <summary>
    /// Invoked when the <see cref="Repository"/> it is registed with completes a checkout operation.
    /// </summary>
    /// <param name="repository">The <see cref="Repository"/> invoking the delegate.</param>
    /// <param name="oldHead">The <see cref="Repository.Head"/> value prior to the checkout operation.</param>
    /// <param name="newHead">The <see cref="Repository.Head"/> value after the checkout operation.</param>
    /// <param name="branchSwitch"></param>
    public delegate void PostCheckoutDelegate(Repository repository, Reference oldHead, Reference newHead, bool branchSwitch);

    /// <summary>
    /// Invoked when the <see cref="Repository"/> it is registered with completes a new commit creation.
    /// </summary>
    /// <param name="repository">The <see cref="Repository"/> invoking the delegate.</param>
    public delegate void PostCommitDelegate(Repository repository);

    /// <summary>
    /// Invoked when the <see cref="Repository"/> it is registered with completes host negotation, but before push begins.
    /// </summary>
    /// <param name="repository">The <see cref="Repository"/> invoking the delegate.</param>
    /// <param name="updates">Enumeration of udpates to be pushed to the remote host.</param>
    /// <returns>True if successful and push should continue, false otherwise.</returns>
    public delegate bool PrePushDelegate(Repository repository, IEnumerable<PushUpdate> updates);
}
