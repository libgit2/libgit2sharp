using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;
using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling the behavior of two trees being merged.
    /// </summary>
    public sealed class MergeTreeOptions : MergeOptionsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MergeTreeOptions"/> class.
        /// <para>
        ///   Default behavior:
        ///     Merge will attempt to find renames.
        /// </para>
        /// </summary>
        public MergeTreeOptions()
        {
        }
    }
}
