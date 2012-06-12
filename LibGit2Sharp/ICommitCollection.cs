using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A collection of commits in a <see cref = "Repository" />.
    /// </summary>
    public interface ICommitCollection : IEnumerable<Commit>
    {
        /// <summary>
        ///   Gets the current sorting strategy applied when enumerating the collection.
        /// </summary>
        GitSortOptions SortedBy { get; }
    }
}
