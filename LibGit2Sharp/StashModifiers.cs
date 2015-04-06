using System;

namespace LibGit2Sharp
{
    ///<summary>
    /// Options controlling Stash behavior.
    ///</summary>
    [Flags]
    public enum StashModifiers
    {
        /// <summary>
        /// Default
        /// </summary>
        Default = 0,

        /// <summary>
        /// All changes already added to the index
        /// are left intact in the working directory
        /// </summary>
        KeepIndex = (1 << 0),

        /// <summary>
        /// All untracked files are also stashed and then
        /// cleaned up from the working directory
        /// </summary>
        IncludeUntracked = (1 << 1),

        /// <summary>
        /// All ignored files are also stashed and then
        /// cleaned up from the working directory
        /// </summary>
        IncludeIgnored = (1 << 2),
    }

    ///<summary>
    /// Options controlling Stash applying behavior.
    ///</summary>
    [Flags]
    public enum StashApplyModifiers
    {
        /// <summary>
        /// Default. Reinstante working directory stashed
        /// changes.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Reinstate both index and working directory stashed
        /// changes.
        /// </summary>
        ReinstateIndex = (1 << 0),
    }
}
