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
        IQueryableCommitCollection Commits { get; }

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
    }
}
