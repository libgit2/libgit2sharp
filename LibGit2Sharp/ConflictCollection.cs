using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///  The collection of <see cref="LibGit2Sharp.Conflict"/>s in a
    ///  <see cref="LibGit2Sharp.Repository"/> index due to a
    ///  previously performed merge operation.
    /// </summary>
    public class ConflictCollection : IEnumerable<Conflict>
    {
        private readonly Repository repo;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected ConflictCollection()
        { }

        internal ConflictCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///  Gets the <see cref="LibGit2Sharp.Conflict"/> for the
        ///  specified relative path.
        /// </summary>
        /// <param name="path">The relative path to query</param>
        /// <returns>A <see cref="Conflict"/> that represents the conflict for this file.</returns>
        public virtual Conflict this[string path]
        {
            get
            {
                return Proxy.git_index_conflict_get(repo.Index.Handle, repo, path);
            }
        }

        #region IEnumerable<IndexEntry> Members

        private List<Conflict> AllConflicts()
        {
            var list = new List<Conflict>();
            IndexEntry ancestor = null, ours = null, theirs = null;
            string currentPath = null;

            foreach (IndexEntry entry in repo.Index)
            {
                if (entry.StageLevel == StageLevel.Staged)
                {
                    continue;
                }

                if (currentPath != null && !entry.Path.Equals(currentPath, StringComparison.Ordinal))
                {
                    list.Add(new Conflict(ancestor, ours, theirs));

                    ancestor = null;
                    ours = null;
                    theirs = null;
                }

                currentPath = entry.Path;

                switch (entry.StageLevel)
                {
                    case StageLevel.Ancestor:
                        ancestor = entry;
                        break;
                    case StageLevel.Ours:
                        ours = entry;
                        break;
                    case StageLevel.Theirs:
                        theirs = entry;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format(
                            CultureInfo.InvariantCulture,
                            "Entry '{0}' bears an unexpected StageLevel '{1}'",
                            entry.Path, entry.StageLevel));
                }
            }

            if (currentPath != null)
            {
                list.Add(new Conflict(ancestor, ours, theirs));
            }

            return list;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Conflict> GetEnumerator()
        {
            return AllConflicts().GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
