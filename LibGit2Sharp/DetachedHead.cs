namespace LibGit2Sharp
{
    internal class DetachedHead : Branch
    {
        internal DetachedHead(Repository repo, Reference reference)
            : base(repo, reference, "(no branch)")
        {
        }

        protected override string Shorten()
        {
            return CanonicalName;
        }

        /// <summary>
        ///   Gets the remote branch which is connected to this local one, or null if there is none.
        /// </summary>
        public override Branch TrackedBranch
        {
            get { return null; }
        }
    }
}
