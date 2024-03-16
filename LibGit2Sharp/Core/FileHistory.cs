using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Represents a file-related log of commits beyond renames.
    /// </summary>
    internal class FileHistory : IEnumerable<LogEntry>
    {
        #region Fields

        /// <summary>
        /// The allowed commit sort strategies.
        /// </summary>
        private static readonly List<CommitSortStrategies> AllowedSortStrategies = new List<CommitSortStrategies>
        {
            CommitSortStrategies.Topological,
            CommitSortStrategies.Time,
            CommitSortStrategies.Topological | CommitSortStrategies.Time
        };

        /// <summary>
        /// The repository.
        /// </summary>
        private readonly Repository _repo;

        /// <summary>
        /// The file's path relative to the repository's root.
        /// </summary>
        private readonly string _path;

        /// <summary>
        /// The filter to be used in querying the commit log.
        /// </summary>
        private readonly CommitFilter _queryFilter;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHistory"/> class.
        /// The commits will be enumerated in reverse chronological order.
        /// </summary>
        /// <param name="repo">The repository.</param>
        /// <param name="path">The file's path relative to the repository's root.</param>
        /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
        internal FileHistory(Repository repo, string path)
            : this(repo, path, new CommitFilter())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHistory"/> class.
        /// The given <see cref="CommitFilter"/> instance specifies the commit
        /// sort strategies and range of commits to be considered.
        /// Only the time (corresponding to <code>--date-order</code>) and topological
        /// (coresponding to <code>--topo-order</code>) sort strategies are supported.
        /// </summary>
        /// <param name="repo">The repository.</param>
        /// <param name="path">The file's path relative to the repository's root.</param>
        /// <param name="queryFilter">The filter to be used in querying the commit log.</param>
        /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
        /// <exception cref="ArgumentException">When an unsupported commit sort strategy is specified.</exception>
        internal FileHistory(Repository repo, string path, CommitFilter queryFilter)
        {
            Ensure.ArgumentNotNull(repo, "repo");
            Ensure.ArgumentNotNull(path, "path");
            Ensure.ArgumentNotNull(queryFilter, "queryFilter");

            // Ensure the commit sort strategy makes sense.
            if (!AllowedSortStrategies.Contains(queryFilter.SortBy))
            {
                throw new ArgumentException("Unsupported sort strategy. Only 'Topological', 'Time', or 'Topological | Time' are allowed.",
                                             nameof(queryFilter));
            }

            _repo = repo;
            _path = path;
            _queryFilter = queryFilter;
        }

        #endregion

        #region IEnumerable<LogEntry> Members

        /// <summary>
        /// Gets the <see cref="IEnumerator{LogEntry}"/> that enumerates the
        /// <see cref="LogEntry"/> instances representing the file's history,
        /// including renames (as in <code>git log --follow</code>).
        /// </summary>
        /// <returns>A <see cref="IEnumerator{LogEntry}"/>.</returns>
        public IEnumerator<LogEntry> GetEnumerator()
        {
            return FullHistory(_repo, _path, _queryFilter).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Gets the relevant commits in which the given file was created, changed, or renamed.
        /// </summary>
        /// <param name="repo">The repository.</param>
        /// <param name="path">The file's path relative to the repository's root.</param>
        /// <param name="filter">The filter to be used in querying the commits log.</param>
        /// <returns>A collection of <see cref="LogEntry"/> instances.</returns>
        private static IEnumerable<LogEntry> FullHistory(IRepository repo, string path, CommitFilter filter)
        {
            var map = new Dictionary<Commit, string>();

            foreach (var currentCommit in repo.Commits.QueryBy(filter))
            {
                var currentPath = map.Keys.Count > 0 ? map[currentCommit] : path;
                var currentTreeEntry = currentCommit.Tree[currentPath];

                if (currentTreeEntry == null)
                {
                    yield break;
                }

                var parentCount = currentCommit.Parents.Count();
                if (parentCount == 0)
                {
                    yield return new LogEntry { Path = currentPath, Commit = currentCommit };
                }
                else
                {
                    DetermineParentPaths(repo, currentCommit, currentPath, map);

                    if (parentCount != 1)
                    {
                        continue;
                    }

                    var parentCommit = currentCommit.Parents.Single();
                    var parentPath = map[parentCommit];
                    var parentTreeEntry = parentCommit.Tree[parentPath];

                    if (parentTreeEntry == null ||
                        parentTreeEntry.Target.Id != currentTreeEntry.Target.Id ||
                        parentPath != currentPath)
                    {
                        yield return new LogEntry { Path = currentPath, Commit = currentCommit };
                    }
                }
            }
        }

        private static void DetermineParentPaths(IRepository repo, Commit currentCommit, string currentPath, IDictionary<Commit, string> map)
        {
            foreach (var parentCommit in currentCommit.Parents.Where(parentCommit => !map.ContainsKey(parentCommit)))
            {
                map.Add(parentCommit, ParentPath(repo, currentCommit, currentPath, parentCommit));
            }
        }

        private static string ParentPath(IRepository repo, Commit currentCommit, string currentPath, Commit parentCommit)
        {
            using (var treeChanges = repo.Diff.Compare<TreeChanges>(parentCommit.Tree, currentCommit.Tree))
            {
                var treeEntryChanges = treeChanges.FirstOrDefault(c => c.Path == currentPath);
                return treeEntryChanges != null && treeEntryChanges.Status == ChangeKind.Renamed
                    ? treeEntryChanges.OldPath
                    : currentPath;
            }
        }
    }
}
