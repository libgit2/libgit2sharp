namespace LibGit2Sharp
{
    /// <summary>
    /// Determines the pending operation of a git repository - ie, whether
    /// an operation (merge, cherry-pick, etc) is in progress.
    /// </summary>
    public enum CurrentOperation
    {
        /// <summary>
        /// No operation is in progress.
        /// </summary>
        None = 0,

        /// <summary>
        /// A merge is in progress.
        /// </summary>
        Merge = 1,

        /// <summary>
        /// A revert is in progress.
        /// </summary>
        Revert = 2,

        /// <summary>
        /// A sequencer revert is in progress.
        /// </summary>
        RevertSequence = 3,

        /// <summary>
        /// A cherry-pick is in progress.
        /// </summary>
        CherryPick = 4,

        /// <summary>
        /// A sequencer cherry-pick is in progress.
        /// </summary>
        CherryPickSequence = 5,

        /// <summary>
        /// A bisect is in progress.
        /// </summary>
        Bisect = 6,

        /// <summary>
        /// A rebase is in progress.
        /// </summary>
        Rebase = 7,

        /// <summary>
        /// A rebase --interactive is in progress.
        /// </summary>
        RebaseInteractive = 8,

        /// <summary>
        /// A rebase --merge is in progress.
        /// </summary>
        RebaseMerge = 9,

        /// <summary>
        /// A mailbox application (am) is in progress.
        /// </summary>
        ApplyMailbox = 10,

        /// <summary>
        /// A mailbox application (am) or rebase is in progress.
        /// </summary>
        ApplyMailboxOrRebase = 11,
    }
}
