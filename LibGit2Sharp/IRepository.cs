using System;

namespace LibGit2Sharp
{
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
    }
}
