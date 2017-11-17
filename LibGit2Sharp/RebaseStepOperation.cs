namespace LibGit2Sharp
{
    /// <summary>
    /// The type of operation to be performed in a rebase step.
    /// </summary>
    public enum RebaseStepOperation
    {
        /// <summary>
        /// Commit is to be cherry-picked.
        /// </summary>
        Pick = 0,

        /// <summary>
        /// Cherry-pick the commit and edit the commit message.
        /// </summary>
        Reword,

        /// <summary>
        /// Cherry-pick the commit but allow user to edit changes.
        /// </summary>
        Edit,

        /// <summary>
        /// Commit is to be squashed into previous commit. The commit
        /// message will be merged with the previous message.
        /// </summary>
        Squash,

        /// <summary>
        /// Commit is to be squashed into previous commit. The commit
        /// message will be discarded.
        /// </summary>
        Fixup,

        /// <summary>
        /// No commit to cherry-pick. Run the given command and continue
        /// if successful.
        /// </summary>
        Exec
    }
}
