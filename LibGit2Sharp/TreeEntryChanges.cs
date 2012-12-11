using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Holds the changes between two versions of a tree entry.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TreeEntryChanges : Changes
    {
        /// <summary>
        ///   Needed for mocking purposes. 
        /// </summary>
        protected TreeEntryChanges()
        { }

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
        public virtual string Path { get; private set; }

        /// <summary>
        ///   The new <see cref="Mode"/>.
        /// </summary>
        public virtual Mode Mode { get; private set; }

        /// <summary>
        ///   The new content hash.
        /// </summary>
        public virtual ObjectId Oid { get; private set; }

        /// <summary>
        ///   The kind of change that has been done (added, deleted, modified ...).
        /// </summary>
        public virtual ChangeKind Status { get; private set; }

        /// <summary>
        ///   The old path.
        /// </summary>
        public virtual string OldPath { get; private set; }

        /// <summary>
        ///   The old <see cref="Mode"/>.
        /// </summary>
        public virtual Mode OldMode { get; private set; }

        /// <summary>
        ///   The old content hash.
        /// </summary>
        public virtual ObjectId OldOid { get; private set; }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "Path = {0}, File {1}",
                    !string.IsNullOrEmpty(Path) ? Path : OldPath, Status);
            }
        }
    }
}
