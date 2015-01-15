using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// Parameters controlling Pull behavior.
    /// </summary>
    public sealed class PullOptions
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PullOptions()
        {
            Fetch = true;
        }

        /// <summary>
        /// Whether a fetch should be performed or not.  This is true by
        /// default, meaning a pull will perform a fetch then merge the
        /// branch that was fetched.  If you have previously performed a
        /// fetch yourself and want to merge that data, this may be false.
        /// </summary>
        public bool Fetch { get; set; }

        /// <summary>
        /// Parameters controlling Fetch behavior.
        /// </summary>
        public FetchOptions FetchOptions { get; set; }

        /// <summary>
        /// Parameters controlling Merge behavior.
        /// </summary>
        public MergeOptions MergeOptions { get; set; }
    }
}
