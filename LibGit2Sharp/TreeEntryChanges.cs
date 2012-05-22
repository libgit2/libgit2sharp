namespace LibGit2Sharp
{
    /// <summary>
    ///   Holds the changes between two versions of a tree entry.
    /// </summary>
    public class TreeEntryChanges : ContentChanges
    {
        internal TreeEntryChanges(string path, Mode mode, ChangeKind status, string oldPath, Mode oldMode, bool isBinaryComparison)
        {
            Path = path;
            Mode = mode;
            Status = status;
            OldPath = oldPath;
            OldMode = oldMode;
            IsBinaryComparison = isBinaryComparison;
        }

        /// <summary>
        ///   The new path.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        ///   The new <see cref="Mode"/>.
        /// </summary>
        public Mode Mode { get; private set; }

        /// <summary>
        ///   The kind of change that has been done (added, deleted, modified ...).
        /// </summary>
        public ChangeKind Status { get; private set; }

        /// <summary>
        ///   The old path.
        /// </summary>
        public string OldPath { get; private set; }

        /// <summary>
        ///   The old <see cref="Mode"/>.
        /// </summary>
        public Mode OldMode { get; private set; }
    }
}
