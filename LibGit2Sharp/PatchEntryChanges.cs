namespace LibGit2Sharp
{
    /// <summary>
    /// Holds the changes between two versions of a file.
    /// </summary>
    public class PatchEntryChanges : ContentChanges
    {
        private readonly TreeEntryChanges treeEntryChanges;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected PatchEntryChanges()
        { }

        internal PatchEntryChanges(bool isBinaryComparison, TreeEntryChanges treeEntryChanges)
            : base(isBinaryComparison)
        {
            this.treeEntryChanges = treeEntryChanges;
        }

        /// <summary>
        /// The new path.
        /// </summary>
        public virtual string Path
        {
            get { return treeEntryChanges.Path; }
        }

        /// <summary>
        /// The new <see cref="Mode"/>.
        /// </summary>
        public virtual Mode Mode
        {
            get { return treeEntryChanges.Mode; }
        }

        /// <summary>
        /// The new content hash.
        /// </summary>
        public virtual ObjectId Oid
        {
            get { return treeEntryChanges.Oid; }
        }

        /// <summary>
        /// The kind of change that has been done (added, deleted, modified ...).
        /// </summary>
        public virtual ChangeKind Status
        {
            get { return treeEntryChanges.Status; }
        }

        /// <summary>
        /// The old path.
        /// </summary>
        public virtual string OldPath
        {
            get { return treeEntryChanges.OldPath; }
        }

        /// <summary>
        /// The old <see cref="Mode"/>.
        /// </summary>
        public virtual Mode OldMode
        {
            get { return treeEntryChanges.OldMode; }
        }

        /// <summary>
        /// The old content hash.
        /// </summary>
        public virtual ObjectId OldOid
        {
            get { return treeEntryChanges.OldOid; }
        }
    }
}
