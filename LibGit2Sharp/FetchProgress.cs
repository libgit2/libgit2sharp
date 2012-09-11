using LibGit2Sharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Contains data regarding fetch progress.
    /// </summary>
    public class FetchProgress : IndexerStats
    {
        /// <summary>
        ///   Fetch progress constructor.
        /// </summary>
        public FetchProgress()
        {
            RemoteCallbacks = new RemoteCallbacks();
        }

        /// <summary>
        ///   Bytes received.
        /// </summary>
        public long Bytes
        {
            get
            {
                return bytes;
            }
        }

        /// <summary>
        ///   The events fired in response to callbacks from libgit2.
        /// </summary>
        public RemoteCallbacks RemoteCallbacks { get; set; }

        internal override void Reset()
        {
            base.Reset();
            bytes = 0;
        }

        #region Fields

        internal long bytes;

        #endregion
    }
}
