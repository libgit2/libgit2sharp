using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Holds the changes between two versions of a tree entry.
    /// </summary>
    public class TreeEntryChanges : Changes
    {
        internal TreeEntryChanges(FilePath path, Mode mode, ObjectId oid, ChangeKind status, FilePath oldPath, Mode oldMode, ObjectId oldOid, bool isBinaryComparison)
        {
            Path = path.Native;
            Mode = mode;
            Oid = oid;
            Status = status;
            OldPath = oldPath.Native;
            OldMode = oldMode;
            OldOid = oldOid;
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
        ///   The new content hash.
        /// </summary>
        public ObjectId Oid { get; private set; }

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
        ///   The old content hash.
        /// </summary>
        public ObjectId OldOid { get; private set; }
    }
}
