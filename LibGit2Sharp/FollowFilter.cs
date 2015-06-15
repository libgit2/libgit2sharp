using System;
using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    /// Criteria used to order the commits of the repository when querying its history.
    /// <para>
    /// The commits will be enumerated from the current HEAD of the repository.
    /// </para>
    /// </summary>
    public sealed class FollowFilter
    {
        private static readonly List<CommitSortStrategies> AllowedSortStrategies = new List<CommitSortStrategies>
        {
            CommitSortStrategies.Topological,
            CommitSortStrategies.Time,
            CommitSortStrategies.Topological | CommitSortStrategies.Time
        };

        private CommitSortStrategies _sortBy;

        /// <summary>
        /// Initializes a new instance of <see cref="FollowFilter" />.
        /// </summary>
        public FollowFilter()
        {
            SortBy = CommitSortStrategies.Time;
        }

        /// <summary>
        /// The ordering strategy to use.
        /// <para>
        /// By default, the commits are shown in reverse chronological order.
        /// </para>
        /// <para>
        /// Only 'Topological', 'Time', or 'Topological | Time' are allowed.
        /// </para>
        /// </summary>
        public CommitSortStrategies SortBy
        {
            get { return _sortBy; }

            set
            {
                if (!AllowedSortStrategies.Contains(value))
                {
                    throw new ArgumentException("Unsupported sort strategy. Only 'Topological', 'Time', or 'Topological | Time' are allowed.",
                                                "value");
                }

                _sortBy = value;
            }
        }
    }
}
