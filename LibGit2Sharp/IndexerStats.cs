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
            gitIndexerStats = new GitIndexerStats();
        }

        /// <summary>
        ///   Total number of objects
        /// </summary>
        public long TotalObjectCount
        {
            get
            {
                return gitIndexerStats.Total;
            }
        }

        /// <summary>
        ///   Number of objects processed.
        /// </summary>
        public long ProcessedObjectCount
        {
            get
            {
                return gitIndexerStats.Processed;
            }
        }

        /// <summary>
        ///   Number of objects received.
        /// </summary>
        public long ReceivedObjectCount
        {
            get
            {
                return gitIndexerStats.Received;
            }
        }

        /// <summary>
        ///   Reset internal data
        /// </summary>
        internal virtual void Reset()
        {
            gitIndexerStats.Processed = 0;
            gitIndexerStats.Total = 0;
            gitIndexerStats.Received = 0;
        }

        #region Fields

        internal GitIndexerStats gitIndexerStats;

        #endregion
    }
}
