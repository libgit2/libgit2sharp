namespace LibGit2Sharp
{
    public interface IContentChanges
    {
        /// <summary>
        ///   Determines if at least one of the compared <see cref="Blob"/>s holds some binary content.
        /// </summary>
        bool IsBinaryComparison { get; }

        /// <summary>
        ///   The number of lines added.
        /// </summary>
        int LinesAdded { get; }

        /// <summary>
        ///   The number of lines deleted.
        /// </summary>
        int LinesDeleted { get; }

        /// <summary>
        ///   The patch corresponding to these changes.
        /// </summary>
        string Patch { get; }
    }
}