using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Wrapper around git_indexer_stats
    /// </summary>
    public class IndexerStats
    {
        /// <summary>
        ///   Constructor
        /// </summary>
        public IndexerStats()
        {
            indexerStats = new GitIndexerStats();
        }
        /// <summary>
        ///   Total number of objects
        /// </summary>
        public long TotalObjectCount
        {
            get
            {
                return indexerStats.Total;
            }
        }

        /// <summary>
        ///   Number of objects processed.
        /// </summary>
        public long ProcessedObjectCount
        {
            get
            {
                return indexerStats.Processed;
            }
        }

        /// <summary>
        ///   Reset internal data
        /// </summary>
        internal virtual void Reset()
        {
            indexerStats.Processed = 0;
            indexerStats.Total = 0;
        }

        #region Fields

        internal GitIndexerStats indexerStats;

        #endregion
    }
}
