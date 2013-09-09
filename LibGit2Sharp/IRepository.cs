using System;
using System.Collections.Generic;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// A Repository is the primary interface into a git repository
    /// </summary>
    public interface IRepository : IDisposable
    {
        /// <summary>
        /// Shortcut to return the branch pointed to by HEAD
        /// </summary>
        Branch Head { get; }

        /// <summary>
        /// Provides access to the configuration settings for this repository.
        /// </summary>
        Configuration Config { get; }

        /// <summary>
        /// Gets the index.
        /// </summary>
        Index Index { get; }

        /// <summary>
        /// Lookup and enumerate references in the repository.
        /// </summary>
        ReferenceCollection Refs { get; }

        /// <summary>
        /// Lookup and enumerate commits in the repository.
        /// Iterating this collection directly starts walking from the HEAD.
        /// </summary>
        IQueryableCommitLog Commits { get; }

        /// <summary>
        /// Lookup and enumerate branches in the repository.
        /// </summary>
        BranchCollection Branches { get; }

        /// <summary>
        /// Lookup and enumerate tags in the repository.
        /// </summary>
        TagCollection Tags { get; }

        /// <summary>
        /// Provides high level information about this repository.
        /// </summary>
        RepositoryInformation Info { get; }

        /// <summary>
        /// Provides access to diffing functionalities to show changes between the working tree and the index or a tree, changes between the index and a tree, changes between two trees, or changes between two files on disk.
        /// </summary>
        Diff Diff {get;}

        /// <summary>
        /// Gets the database.
        /// </summary>
        ObjectDatabase ObjectDatabase { get; }

        /// <summary>
        /// Lookup notes in the repository.
        /// </summary>
        NoteCollection Notes { get; }

        /// <summary>
        /// Submodules in the repository.
        /// </summary>
        SubmoduleCollection Submodules { get; }

        /// <summary>
        /// Checkout the commit pointed at by the tip of the specified <see cref="Branch"/>.
        /// <para>
        ///   If this commit is the current tip of the branch as it exists in the repository, the HEAD
        ///   will point to this branch. Otherwise, the HEAD will be detached, pointing at the commit sha.
        /// </para>
        /// </summary>
        /// <param name="branch">The <see cref="Branch"/> to check out.</param>
        /// <param name="checkoutModifiers"><see cref="CheckoutModifiers"/> controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress"><see cref="CheckoutProgressHandler"/> that checkout progress is reported through.</param>
        /// <param name="checkoutNotificationOptions"><see cref="CheckoutNotificationOptions"/> to manage checkout notifications.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        Branch Checkout(Branch branch, CheckoutModifiers checkoutModifiers, CheckoutProgressHandler onCheckoutProgress, CheckoutNotificationOptions checkoutNotificationOptions);

        /// <summary>
        /// Checkout the specified branch, reference or SHA.
        /// <para>
        ///   If the committishOrBranchSpec parameter resolves to a branch name, then the checked out HEAD will
        ///   will point to the branch. Otherwise, the HEAD will be detached, pointing at the commit sha.
        /// </para>
        /// </summary>
        /// <param name="committishOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <param name="checkoutModifiers">Options controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress">Callback method to report checkout progress updates through.</param>
        /// <param name="checkoutNotificationOptions"><see cref="CheckoutNotificationOptions"/> to manage checkout notifications.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        Branch Checkout(string committishOrBranchSpec, CheckoutModifiers checkoutModifiers, CheckoutProgressHandler onCheckoutProgress, CheckoutNotificationOptions checkoutNotificationOptions);

        /// <summary>
        /// Checkout the specified <see cref="Commit"/>.
        /// <para>
        ///   Will detach the HEAD and make it point to this commit sha.
        /// </para>
        /// </summary>
        /// <param name="commit">The <see cref="Commit"/> to check out.</param>
        /// <param name="checkoutModifiers"><see cref="CheckoutModifiers"/> controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress"><see cref="CheckoutProgressHandler"/> that checkout progress is reported through.</param>
        /// <param name="checkoutNotificationOptions"><see cref="CheckoutNotificationOptions"/> to manage checkout notifications.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        Branch Checkout(Commit commit, CheckoutModifiers checkoutModifiers, CheckoutProgressHandler onCheckoutProgress, CheckoutNotificationOptions checkoutNotificationOptions);

        /// <summary>
        /// Updates specifed paths in the index and working directory with the versions from the specified branch, reference, or SHA.
        /// <para>
        /// This method does not switch branches or update the current repository HEAD.
        /// </para>
        /// </summary>
        /// <param name = "committishOrBranchSpec">A revparse spec for the commit or branch to checkout paths from.</param>
        /// <param name="paths">The paths to checkout.</param>
        /// <param name="checkoutOptions">Collection of parameters controlling checkout behavior.</param>
        void CheckoutPaths(string committishOrBranchSpec, IEnumerable<string> paths, CheckoutOptions checkoutOptions = null);

        /// <summary>
        /// Try to lookup an object by its <see cref="ObjectId"/>. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name="id">The id to lookup.</param>
        /// <returns>The <see cref="GitObject"/> or null if it was not found.</returns>
        GitObject Lookup(ObjectId id);

        /// <summary>
        /// Try to lookup an object by its sha or a reference canonical name. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name="objectish">A revparse spec for the object to lookup.</param>
        /// <returns>The <see cref="GitObject"/> or null if it was not found.</returns>
        GitObject Lookup(string objectish);

        /// <summary>
        /// Try to lookup an object by its <see cref="ObjectId"/> and <see cref="ObjectType"/>. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name="id">The id to lookup.</param>
        /// <param name="type">The kind of GitObject being looked up</param>
        /// <returns>The <see cref="GitObject"/> or null if it was not found.</returns>
        GitObject Lookup(ObjectId id, ObjectType type);

        /// <summary>
        /// Try to lookup an object by its sha or a reference canonical name and <see cref="ObjectType"/>. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name="objectish">A revparse spec for the object to lookup.</param>
        /// <param name="type">The kind of <see cref="GitObject"/> being looked up</param>
        /// <returns>The <see cref="GitObject"/> or null if it was not found.</returns>
        GitObject Lookup(string objectish, ObjectType type);

        /// <summary>
        /// Stores the content of the <see cref="Repository.Index"/> as a new <see cref="Commit"/> into the repository.
        /// The tip of the <see cref="Repository.Head"/> will be used as the parent of this new Commit.
        /// Once the commit is created, the <see cref="Repository.Head"/> will move forward to point at it.
        /// </summary>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <param name="author">The <see cref="Signature"/> of who made the change.</param>
        /// <param name="committer">The <see cref="Signature"/> of who added the change to the repository.</param>
        /// <param name="amendPreviousCommit">True to amend the current <see cref="Commit"/> pointed at by <see cref="Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref="Commit"/>.</returns>
        Commit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false);

        /// <summary>
        /// Sets the current <see cref="Head"/> to the specified commit and optionally resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="resetOptions">Flavor of reset operation to perform.</param>
        /// <param name="commit">The target commit object.</param>
        void Reset(ResetOptions resetOptions, Commit commit);

        /// <summary>
        /// Replaces entries in the <see cref="Repository.Index"/> with entries from the specified commit.
        /// </summary>
        /// <param name="commit">The target commit object.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be considered.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        void Reset(Commit commit, IEnumerable<string> paths = null, ExplicitPathsOptions explicitPathsOptions = null);

        /// <summary>
        /// Clean the working tree by removing files that are not under version control.
        /// </summary>
        void RemoveUntrackedFiles();

        /// <summary>
        /// Gets the references to the tips that are currently being merged.
        /// </summary>
        IEnumerable<MergeHead> MergeHeads { get; }

        /// <summary>
        /// Provides access to network functionality for a repository.
        /// </summary>
        Network Network { get; }
    }
}
