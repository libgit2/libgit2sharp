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
        IEnumerable<LogEntry> QueryBy(string path, CommitFilter filter);

    }
}
