namespace LibGit2Sharp
{
    /// <summary>
    /// An entry in a file's commit history.
    /// </summary>
    public sealed class LogEntry
    {
        /// <summary>
        /// The file's path relative to the repository's root.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The commit in which the file was created or changed.
        /// </summary>
        public Commit Commit { get; set; }
    }
}
