﻿using System;

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
        protected MergeOptionsBase()
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
        /// If set, do not create or return conflict entries, but stop and return
        /// an error result after finding the first conflict.
        /// </summary>
        public bool FailOnConflict { get; set; }

        /// <summary>
        /// Do not write the Resolve Undo Cache extension on the generated index. This can
        /// be useful when no merge resolution will be presented to the user (e.g. a server-side
        /// merge attempt).
        /// </summary>
        public bool SkipReuc { get; set; }

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

        /// <summary>
        /// Ignore changes in amount of whitespace
        /// </summary>
        public bool IgnoreWhitespaceChange { get; set; }
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

    /// <summary>
    /// File merging flags.
    /// </summary>
    [Flags]
    public enum MergeFileFlag
    {
        /// <summary>
        /// Defaults
        /// </summary>
        Default = 0,

        /// <summary>
        /// Create standard conflicted merge files
        /// </summary>
        StyleMerge = (1 << 0),

        /// <summary>
        /// Create diff3-style files
        /// </summary>
        Diff3 = (1 << 1),

        /// <summary>
        /// Condense non-alphanumeric regions for simplified diff file
        /// </summary>
        SimplifyAlnum = (1 << 2),

        /// <summary>
        /// Ignore all whitespace
        /// </summary>
        IgnoreWhitespace = (1 << 3),

        /// <summary>
        /// Ignore changes in amount of whitespace
        /// </summary>
        IgnoreWhitespaceChange = (1 << 4),

        /// <summary>
        /// Ignore whitespace at end of line
        /// </summary>
        IgnoreWhitespace_Eol = (1 << 5),

        /// <summary>
        /// Use the "patience diff" algorithm
        /// </summary>
        DiffPatience = (1 << 6),

        /// <summary>
        /// Take extra time to find minimal diff
        /// </summary>
        DiffMinimal = (1 << 7),
    }
}
