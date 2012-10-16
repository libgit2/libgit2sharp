using LibGit2Sharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Contains data regarding fetch progress.
    /// </summary>
    public class FetchProgress
    {
        /// <summary>
        ///   Fetch progress constructor.
        /// </summary>
        public FetchProgress()
        {
            IndexerStats = new IndexerStats();
        }

        /// <summary>
        ///   Bytes received.
        /// </summary>
        public long Bytes
        {
            get
            {
                // read the bytes atomically
                return Interlocked.Read(ref bytes);
            }
        }

        /// <summary>
        ///   The IndexerStats
        /// </summary>
        public IndexerStats IndexerStats { get;  private set; }
        
        internal void Reset()
        {
            IndexerStats.Reset();
            bytes = 0;
        }

        #region Fields

        internal long bytes;

        #endregion
    }
}
