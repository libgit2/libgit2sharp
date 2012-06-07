namespace LibGit2Sharp
{
    public interface ITreeEntryChanges : IContentChanges
    {
        /// <summary>
        ///   The new path.
        /// </summary>
        string Path { get; }

        /// <summary>
        ///   The new <see cref="Mode"/>.
        /// </summary>
        Mode Mode { get; }

        /// <summary>
        ///   The kind of change that has been done (added, deleted, modified ...).
        /// </summary>
        ChangeKind Status { get; }

        /// <summary>
        ///   The old path.
        /// </summary>
        string OldPath { get; }

        /// <summary>
        ///   The old <see cref="Mode"/>.
        /// </summary>
        Mode OldMode { get; }
    }
}