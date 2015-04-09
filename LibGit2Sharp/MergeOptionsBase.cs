using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;
using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling the behavior of actions that use merge (merge
    /// proper, cherry-pick, revert)
    /// </summary>
    public abstract class MergeOptionsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MergeOptionsBase"/> class.
        /// The default behavior is to attempt to find renames.
        /// </summary>
        public MergeOptionsBase()
        {
            FindRenames = true;
            RenameThreshold = 50;
            TargetLimit = 200;
        }

        /// <summary>
        /// Find renames. Default is true.
        /// </summary>
        public bool FindRenames { get; set; }

        /// <summary>
        /// Similarity to consider a file renamed.
        /// </summary>
        public int RenameThreshold;

        /// <summary>
        /// Maximum similarity sources to examine (overrides
        /// 'merge.renameLimit' config (default 200)
        /// </summary>
        public int TargetLimit;

        /// <summary>
        /// How to handle conflicts encountered during a merge.
        /// </summary>
        public MergeFileFavor MergeFileFavor { get; set; }
    }

    /// <summary>
    /// Enum specifying how merge should deal with conflicting regions
    /// of the files.
    /// </summary>
    public enum MergeFileFavor
    {
        /// <summary>
        /// When a region of a file is changed in both branches, a conflict
        /// will be recorded in the index so that the checkout operation can produce
        /// a merge file with conflict markers in the working directory.
        /// This is the default.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// When a region of a file is changed in both branches, the file
        /// created in the index will contain the "ours" side of any conflicting
        /// region. The index will not record a conflict.
        /// </summary>
        Ours = 1,

        /// <summary>
        /// When a region of a file is changed in both branches, the file
        /// created in the index will contain the "theirs" side of any conflicting
        /// region. The index will not record a conflict.
        /// </summary>
        Theirs = 2,

        /// <summary>
        /// When a region of a file is changed in both branches, the file
        /// created in the index will contain each unique line from each side,
        /// which has the result of combining both files. The index will not
        /// record a conflict.
        /// </summary>
        Union = 3,
    }
}
