using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LibGit2Sharp
{
    internal class EmptyCommitCollection : IQueryableCommitCollection
    {
        internal EmptyCommitCollection(GitSortOptions sortedBy)
        {
            SortedBy = sortedBy;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<Commit> GetEnumerator()
        {
            return Enumerable.Empty<Commit>().GetEnumerator();
        }

        /// <summary>
        ///  Returns the list of commits of the repository matching the specified <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">The options used to control which commits will be returned.</param>
        /// <returns>A collection of commits, ready to be enumerated.</returns>
        public ICommitCollection QueryBy(Filter filter)
        {
            return new EmptyCommitCollection(filter.SortBy);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the current sorting strategy applied when enumerating the collection.
        /// </summary>
        public GitSortOptions SortedBy { get; private set; }
    }
}