using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A log of commits in a <see cref="Repository"/>
    /// </summary>
    public sealed class CommitLog : IQueryableCommitLog
    {
        private readonly Repository repo;
        private readonly CommitFilter queryFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitLog"/> class.
        /// The commits will be enumerated according in reverse chronological order.
        /// </summary>
        /// <param name="repo">The repository.</param>
        internal CommitLog(Repository repo)
            : this(repo, new CommitFilter())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitLog"/> class.
        /// </summary>
        /// <param name="repo">The repository.</param>
        /// <param name="queryFilter">The filter to use in querying commits</param>
        internal CommitLog(Repository repo, CommitFilter queryFilter)
        {
            this.repo = repo;
            this.queryFilter = queryFilter;
        }

        /// <summary>
        /// Gets the current sorting strategy applied when enumerating the log
        /// </summary>
        public CommitSortStrategies SortedBy
        {
            get { return queryFilter.SortBy; }
        }

        #region IEnumerable<Commit> Members

        /// <summary>
        /// Returns an enumerator that iterates through the log.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the log.</returns>
        public IEnumerator<Commit> GetEnumerator()
        {
            return new CommitEnumerator(repo, queryFilter);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the log.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the log.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Returns the list of commits of the repository matching the specified <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">The options used to control which commits will be returned.</param>
        /// <returns>A list of commits, ready to be enumerated.</returns>
        public ICommitLog QueryBy(CommitFilter filter)
        {
            Ensure.ArgumentNotNull(filter, "filter");
            Ensure.ArgumentNotNull(filter.IncludeReachableFrom, "filter.IncludeReachableFrom");
            Ensure.ArgumentNotNullOrEmptyString(filter.IncludeReachableFrom.ToString(), "filter.IncludeReachableFrom");

            return new CommitLog(repo, filter);
        }

        /// <summary>
        /// Returns the list of commits of the repository representing the history of a file beyond renames.
        /// </summary>
        /// <param name="path">The file's path.</param>
        /// <returns>A list of file history entries, ready to be enumerated.</returns>
        public IEnumerable<LogEntry> QueryBy(string path)
        {
            Ensure.ArgumentNotNull(path, "path");

            return new FileHistory(repo, path);
        }

        /// <summary>
        /// Returns the list of commits of the repository representing the history of a file beyond renames.
        /// </summary>
        /// <param name="path">The file's path.</param>
        /// <param name="filter">The options used to control which commits will be returned.</param>
        /// <returns>A list of file history entries, ready to be enumerated.</returns>
        public IEnumerable<LogEntry> QueryBy(string path, CommitFilter filter)
        {
            Ensure.ArgumentNotNull(path, "path");
            Ensure.ArgumentNotNull(filter, "filter");

            return new FileHistory(repo, path, filter);
        }

        private class CommitEnumerator : IEnumerator<Commit>
        {
            private readonly Repository repo;
            private readonly RevWalker walker;
            private ObjectId currentOid;

            public CommitEnumerator(Repository repo, CommitFilter filter)
            {
                this.repo = repo;

                walker = new RevWalker(repo);

                walker.Sorting(filter.SortBy);

                foreach (ObjectId actedOn in repo.Committishes(filter.SinceList).TakeWhile(o => o != null))
                {
                    walker.Push(actedOn);
                }

                if(filter.UntilList != null)
                {
                    foreach (ObjectId actedOn in repo.Committishes(filter.UntilList).TakeWhile(o => o != null))
                    {
                        walker.Hide(actedOn);
                    }
                }

                if (filter.FirstParentOnly)
                {
                    walker.SimplifyFirstParent();
                }
            }

            #region IEnumerator<Commit> Members

            public Commit Current
            {
                get { return repo.Lookup<Commit>(currentOid); }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                ObjectId id = walker.Next();
                if (id == null)
                {
                    return false;
                }

                currentOid = id;
                return true;
            }

            public void Reset()
            {
                walker.Reset();
            }

            #endregion

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                walker.SafeDispose();
            }
        }
    }

    /// <summary>
    /// Determines the finding strategy of merge base.
    /// </summary>
    public enum MergeBaseFindingStrategy
    {
        /// <summary>
        /// Compute the best common ancestor between some commits to use in a three-way merge.
        /// <para>
        /// When more than two commits are provided, the computation is performed between the first commit and a hypothetical merge commit across all the remaining commits.
        /// </para>
        /// </summary>
        Standard,
        /// <summary>
        /// Compute the best common ancestor of all supplied commits, in preparation for an n-way merge.
        /// </summary>
        Octopus,
    }
}
