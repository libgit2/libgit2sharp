namespace LibGit2Sharp
{
    /// <summary>
    /// Provides optional additional information to commit creation.
    /// By default, a new commit will be created (instead of amending the
    /// HEAD commit) and an empty commit which is unchanged from the current
    /// HEAD is disallowed.
    /// </summary>
    public sealed class CommitOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitOptions"/> class.
        /// <para>
        ///   Default behavior:
        ///     The message is prettified.
        ///     No automatic removal of comments is performed.
        /// </para>
        /// </summary>
        public CommitOptions()
        {
            PrettifyMessage = true;
        }

        /// <summary>
        /// True to amend the current <see cref="Commit"/> pointed at by <see cref="Repository.Head"/>, false otherwise.
        /// </summary>
        public bool AmendPreviousCommit { get; set; }

        /// <summary>
        /// True to allow creation of an empty <see cref="Commit"/>, false otherwise.
        /// </summary>
        public bool AllowEmptyCommit { get; set; }

        /// <summary>
        /// True to prettify the message by stripping leading and trailing empty lines, trailing whitespace, and collapsing consecutive empty lines, false otherwise.
        /// </summary>
        public bool PrettifyMessage { get; set; }

        /// <summary>
        /// The starting line char used to identify commentaries in the Commit message during the prettifying of the Commit message. If set (usually to '#'), all lines starting with this char will be removed from the message before the Commit is done.
        /// This property will only be considered when PrettifyMessage is set to true.
        /// </summary>
        public char? CommentaryChar { get; set; }
    }
}
