using System.Collections.Generic;

namespace LibGit2Sharp
{
    public interface ICommitCollection : IEnumerable<Commit>
    {
        /// <summary>
        /// Gets the current sorting strategy applied when enumerating the collection
        /// </summary>
        GitSortOptions SortedBy { get; }
    }
}