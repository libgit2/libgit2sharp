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
        /// Find the best possible merge base given two <see cref="Commit"/>s.
        /// </summary>
        /// <param name="first">The first <see cref="Commit"/>.</param>
        /// <param name="second">The second <see cref="Commit"/>.</param>
        /// <returns>The merge base or null if none found.</returns>
        Commit FindMergeBase(Commit first, Commit second);

        /// <summary>
        /// Find the best possible merge base given two or more <see cref="Commit"/> according to the <see cref="MergeBaseFindingStrategy"/>.
        /// </summary>
        /// <param name="commits">The <see cref="Commit"/>s for which to find the merge base.</param>
        /// <param name="strategy">The strategy to leverage in order to find the merge base.</param>
        /// <returns>The merge base or null if none found.</returns>
        Commit FindMergeBase(IEnumerable<Commit> commits, MergeBaseFindingStrategy strategy);
    }
}
