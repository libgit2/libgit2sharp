﻿using System;
using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A log of commits in a <see cref = "Repository" /> that can be filtered with queries.
    /// </summary>
    public interface IQueryableCommitLog : ICommitLog, IQueryableCommitCollection
    { }

    /// <summary>
    ///   A collection of commits in a <see cref = "Repository" /> that can be filtered with queries.
    /// </summary>
    [Obsolete("This interface will be removed in the next release. Please use IQueryableCommitLog instead.")]
    public interface IQueryableCommitCollection : ICommitCollection
    {
        /// <summary>
        ///   Returns the list of commits of the repository matching the specified <paramref name = "filter" />.
        /// </summary>
        /// <param name = "filter">The options used to control which commits will be returned.</param>
        /// <returns>A list of commits, ready to be enumerated.</returns>
        ICommitLog QueryBy(Filter filter);

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
        [Obsolete("This method will be removed in the next release. Please use Repository.Commit() instead.")]
        ICommit Create(string message, Signature author, Signature committer, bool amendPreviousCommit);

        /// <summary>
        ///   Find the best possible common ancestor given two <see cref = "Commit"/>s.
        /// </summary>
        /// <param name = "first">The first <see cref = "Commit"/>.</param>
        /// <param name = "second">The second <see cref = "Commit"/>.</param>
        /// <returns>The common ancestor or null if none found.</returns>
        ICommit FindCommonAncestor(ICommit first, ICommit second);

        /// <summary>
        ///   Find the best possible common ancestor given two or more <see cref = "Commit"/>s.
        /// </summary>
        /// <param name = "commits">The <see cref = "Commit"/> for which to find the common ancestor.</param>
        /// <returns>The common ancestor or null if none found.</returns>
        ICommit FindCommonAncestor(IEnumerable<ICommit> commits);
    }
}
