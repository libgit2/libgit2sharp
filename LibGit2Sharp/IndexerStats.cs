using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp.Core
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
                return indexerStats.total;
            }
        }

        /// <summary>
        ///   Number of objects processed.
        /// </summary>
        public long ProcessedObjectCount
        {
            get
            {
                return indexerStats.processed;
            }
        }

        /// <summary>
        ///   Reset internal data
        /// </summary>
        internal virtual void Reset()
        {
            indexerStats.processed = 0;
            indexerStats.total = 0;
        }

        #region Fields

        internal GitIndexerStats indexerStats;

        #endregion
    }
}
