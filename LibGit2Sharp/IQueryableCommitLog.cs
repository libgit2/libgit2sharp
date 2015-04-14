using System;
using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    /// A log of commits in a <see cref="Repository"/> that can be filtered with queries.
    /// </summary>
    public interface IQueryableCommitLog : ICommitLog
    {
        /// <summary>
        /// Returns the list of commits of the repository matching the specified <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">The options used to control which commits will be returned.</param>
        /// <returns>A list of commits, ready to be enumerated.</returns>
        ICommitLog QueryBy(CommitFilter filter);

        /// <summary>
        /// Returns the list of commits of the repository representing the history of a file beyond renames.
        /// </summary>
        /// <param name="path">The file's path.</param>
        /// <returns>A list of file history entries, ready to be enumerated.</returns>
        IEnumerable<LogEntry> QueryBy(string path);

        /// <summary>
        /// Returns the list of commits of the repository representing the history of a file beyond renames.
        /// </summary>
        /// <param name="path">The file's path.</param>
        /// <param name="filter">The options used to control which commits will be returned.</param>
        /// <returns>A list of file history entries, ready to be enumerated.</returns>
        IEnumerable<LogEntry> QueryBy(string path, FollowFilter filter);

        /// <summary>
        /// Find the best possible merge base given two <see cref="Commit"/>s.
        /// </summary>
        /// <param name="first">The first <see cref="Commit"/>.</param>
        /// <param name="second">The second <see cref="Commit"/>.</param>
        /// <returns>The merge base or null if none found.</returns>
        [Obsolete("This method will be removed in the next release. Please use ObjectDatabase.FindMergeBase() instead.")]
        Commit FindMergeBase(Commit first, Commit second);

        /// <summary>
        /// Find the best possible merge base given two or more <see cref="Commit"/> according to the <see cref="MergeBaseFindingStrategy"/>.
        /// </summary>
        /// <param name="commits">The <see cref="Commit"/>s for which to find the merge base.</param>
        /// <param name="strategy">The strategy to leverage in order to find the merge base.</param>
        /// <returns>The merge base or null if none found.</returns>
        [Obsolete("This method will be removed in the next release. Please use ObjectDatabase.FindMergeBase() instead.")]
        Commit FindMergeBase(IEnumerable<Commit> commits, MergeBaseFindingStrategy strategy);
    }
}
