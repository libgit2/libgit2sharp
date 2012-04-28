using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Holds the changes between two versions of a tree entry.
    /// </summary>
    public class TreeEntryChanges
    {
        private readonly StringBuilder patchBuilder = new StringBuilder();

        internal TreeEntryChanges(string path, Mode mode, ChangeKind status, string oldPath, Mode oldMode)
        {
            Path = path;
            Mode = mode;
            Status = status;
            OldPath = oldPath;
            OldMode = oldMode;
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

        /// <summary>
        ///   The number of lines added.
        /// </summary>
        public int LinesAdded { get; internal set; }

        /// <summary>
        ///   The number of lines deleted.
        /// </summary>
        public int LinesDeleted { get; internal set; }

        /// <summary>
        ///   The  patch corresponding to these changes.
        /// </summary>
        public string Patch
        {
            get { return patchBuilder.ToString(); }
        }

        internal StringBuilder PatchBuilder
        {
            get { return patchBuilder; }
        }
    }
}
