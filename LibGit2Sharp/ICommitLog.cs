using System;
using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A log of commits in a <see cref = "Repository" />.
    /// </summary>
    public interface ICommitLog : ICommitCollection
    { }

    /// <summary>
    ///   A collection of commits in a <see cref = "Repository" />.
    /// </summary>
    [Obsolete("This interface will be removed in the next release. Please use ICommitLog instead.")]
    public interface ICommitCollection : IEnumerable<Commit>
    {
        /// <summary>
        ///   Gets the current sorting strategy applied when enumerating the log.
        /// </summary>
        GitSortOptions SortedBy { get; }
    }
}
