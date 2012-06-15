using System.Collections.Generic;

namespace LibGit2Sharp
{
    public interface ITreeChanges : IEnumerable<TreeEntryChanges>
    {
        /// <summary>
        ///   Gets the <see cref = "TreeEntryChanges"/> corresponding to the specified <paramref name = "path"/>.
        /// </summary>
        TreeEntryChanges this[string path] { get; }

        /// <summary>
        ///   List of <see cref = "TreeEntryChanges"/> that have been been added.
        /// </summary>
        IEnumerable<TreeEntryChanges> Added { get; }

        /// <summary>
        ///   List of <see cref = "TreeEntryChanges"/> that have been deleted.
        /// </summary>
        IEnumerable<TreeEntryChanges> Deleted { get; }

        /// <summary>
        ///   List of <see cref = "TreeEntryChanges"/> that have been modified.
        /// </summary>
        IEnumerable<TreeEntryChanges> Modified { get; }

        /// <summary>
        ///   The total number of lines added in this diff.
        /// </summary>
        int LinesAdded { get; }

        /// <summary>
        ///   The total number of lines added in this diff.
        /// </summary>
        int LinesDeleted { get; }

        /// <summary>
        ///   The full patch file of this diff.
        /// </summary>
        string Patch { get; }
    }
}