using System;
using System.Collections.Generic;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Repository is the primary interface into a git repository
    /// </summary>
    public interface IRepository : IDisposable
    {
        /// <summary>
        ///   Shortcut to return the branch pointed to by HEAD
        /// </summary>
        /// <returns></returns>
        Branch Head { get; }

        /// <summary>
        ///   Provides access to the configuration settings for this repository.
        /// </summary>
        Configuration Config { get; }

        /// <summary>
        ///   Gets the index.
        /// </summary>
        Index Index { get; }

        /// <summary>
        ///   Lookup and enumerate references in the repository.
        /// </summary>
        ReferenceCollection Refs { get; }

        /// <summary>
        ///   Lookup and manage remotes in the repository.
        /// </summary>
        RemoteCollection Remotes { get; }

        /// <summary>
        ///   Lookup and enumerate commits in the repository.
        ///   Iterating this collection directly starts walking from the HEAD.
        /// </summary>
        IQueryableCommitLog Commits { get; }

        /// <summary>
        ///   Lookup and enumerate branches in the repository.
        /// </summary>
        BranchCollection Branches { get; }

        /// <summary>
        ///   Lookup and enumerate tags in the repository.
        /// </summary>
        TagCollection Tags { get; }

        /// <summary>
        ///   Provides high level information about this repository.
        /// </summary>
        RepositoryInformation Info { get; }

        /// <summary>
        ///   Provides access to diffing functionalities to show changes between the working tree and the index or a tree, changes between the index and a tree, changes between two trees, or changes between two files on disk.
        /// </summary>
        Diff Diff {get;}

        /// <summary>
        ///   Gets the database.
        /// </summary>
        ObjectDatabase ObjectDatabase { get; }

        /// <summary>
        ///   Lookup notes in the repository.
        /// </summary>
        NoteCollection Notes { get; }

        /// <summary>
        ///   Checkout the specified <see cref = "Branch" />.
        /// </summary>
        /// <param name="branch">The <see cref = "Branch" /> to check out. </param>
        /// <param name="checkoutOptions"><see cref = "CheckoutOptions" /> controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress"><see cref = "CheckoutProgressHandler" /> that checkout progress is reported through.</param>
        /// <returns>The <see cref = "Branch" /> that was checked out.</returns>
        Branch Checkout(Branch branch, CheckoutOptions checkoutOptions, CheckoutProgressHandler onCheckoutProgress);

        /// <summary>
        ///   Checkout the specified branch, reference or SHA.
        /// </summary>
        /// <param name = "committishOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <param name="checkoutOptions">Options controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress">Callback method to report checkout progress updates through.</param>
        /// <returns>The new HEAD.</returns>
        Branch Checkout(string committishOrBranchSpec, CheckoutOptions checkoutOptions, CheckoutProgressHandler onCheckoutProgress);

        /// <summary>
        ///   Try to lookup an object by its <see cref = "ObjectId" /> and <see cref = "GitObjectType" />. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name = "id">The id to lookup.</param>
        /// <param name = "type">The kind of GitObject being looked up</param>
        /// <returns>The <see cref = "GitObject" /> or null if it was not found.</returns>
        GitObject Lookup(ObjectId id, GitObjectType type = GitObjectType.Any);

        /// <summary>
        ///   Try to lookup an object by its sha or a reference canonical name and <see cref = "GitObjectType" />. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name = "objectish">A revparse spec for the object to lookup.</param>
        /// <param name = "type">The kind of <see cref = "GitObject" /> being looked up</param>
        /// <returns>The <see cref = "GitObject" /> or null if it was not found.</returns>
        GitObject Lookup(string objectish, GitObjectType type = GitObjectType.Any);

        /// <summary>
        ///   Stores the content of the <see cref = "Repository.Index" /> as a new <see cref = "Commit" /> into the repository.
        ///   The tip of the <see cref = "Repository.Head"/> will be used as the parent of this new Commit.
        ///   Once the commit is created, the <see cref = "Repository.Head"/> will move forward to point at it.
        /// </summary>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <param name = "author">The <see cref = "Signature" /> of who made the change.</param>
        /// <param name = "committer">The <see cref = "Signature" /> of who added the change to the repository.</param>
        /// <param name = "amendPreviousCommit">True to amend the current <see cref = "Commit"/> pointed at by <see cref = "Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref = "Commit" />.</returns>
        Commit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false);

        /// <summary>
        ///   Sets the current <see cref = "Head" /> to the specified commit and optionally resets the <see cref = "Index" /> and
        ///   the content of the working tree to match.
        /// </summary>
        /// <param name = "resetOptions">Flavor of reset operation to perform.</param>
        /// <param name = "commit">The target commit object.</param>
        void Reset(ResetOptions resetOptions, Commit commit);

        /// <summary>
        ///   Replaces entries in the <see cref="Repository.Index"/> with entries from the specified commit.
        /// </summary>
        /// <param name = "commit">The target commit object.</param>
        /// <param name = "paths">The list of paths (either files or directories) that should be considered.</param>
        void Reset(Commit commit, IEnumerable<string> paths = null);

        /// <summary>
        /// Clean the working tree by removing files that are not under version control.
        /// </summary>
        void RemoveUntrackedFiles();
    }
}
