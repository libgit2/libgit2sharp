using System;

namespace LibGit2Sharp
{
    public interface IRepository : IDisposable
    {
        /// <summary>
        ///   Shortcut to return the branch pointed to by HEAD
        /// </summary>
        /// <returns></returns>
        IBranch Head { get; }

        /// <summary>
        ///   Provides access to the configuration settings for this repository.
        /// </summary>
        IConfiguration Config { get; }

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
        IRemoteCollection Remotes { get; }

        /// <summary>
        ///   Lookup and enumerate commits in the repository.
        ///   Iterating this collection directly starts walking from the HEAD.
        /// </summary>
        IQueryableCommitLog Commits { get; }

        /// <summary>
        ///   Lookup and enumerate branches in the repository.
        /// </summary>
        IBranchCollection Branches { get; }

        /// <summary>
        ///   Lookup and enumerate tags in the repository.
        /// </summary>
        TagCollection Tags { get; }

        /// <summary>
        ///   Provides high level information about this repository.
        /// </summary>
        IRepositoryInformation Info { get; }

        /// <summary>
        ///   Provides access to diffing functionalities to show changes between the working tree and the index or a tree, changes between the index and a tree, changes between two trees, or changes between two files on disk.
        /// </summary>
        IDiff Diff {get;}

        /// <summary>
        ///   Checkout the specified branch.
        /// </summary>
        /// <param name="branch">The branch to checkout.</param>
        /// <returns>The branch.</returns>
        IBranch Checkout(IBranch branch);

        /// <summary>
        ///   Checkout the specified branch, reference or SHA.
        /// </summary>
        /// <param name = "shaOrReferenceName">The sha of the commit, a canonical reference name or the name of the branch to checkout.</param>
        /// <returns>The new HEAD.</returns>
        IBranch Checkout(string shaOrReferenceName);

        /// <summary>
        ///   Try to lookup an object by its <see cref = "ObjectId" /> and <see cref = "GitObjectType" />. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name = "id">The id to lookup.</param>
        /// <param name = "type">The kind of GitObject being looked up</param>
        /// <returns>The <see cref = "GitObject" /> or null if it was not found.</returns>
        IGitObject Lookup(ObjectId id, GitObjectType type = GitObjectType.Any);

        /// <summary>
        ///   Try to lookup an object by its sha or a reference canonical name and <see cref = "GitObjectType" />. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name = "shaOrReferenceName">The sha or reference canonical name to lookup.</param>
        /// <param name = "type">The kind of <see cref = "GitObject" /> being looked up</param>
        /// <returns>The <see cref = "GitObject" /> or null if it was not found.</returns>
        IGitObject Lookup(string shaOrReferenceName, GitObjectType type = GitObjectType.Any);

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
        ICommit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false);
    }
}
