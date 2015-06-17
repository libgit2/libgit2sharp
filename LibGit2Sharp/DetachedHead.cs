namespace LibGit2Sharp
{
    internal class DetachedHead : Branch
    {
        internal DetachedHead(Repository repo, Reference reference)
            : base(repo,
                   reference,
                   reference == null || reference.TargetIdentifier == null || reference.TargetIdentifier.Length < 7
                      ? "(no branch)"
                      : string.Format("detatched from {0}", reference.TargetIdentifier.Substring(0, 7)))
        { }

        protected override string Shorten()
        {
            return CanonicalName;
        }

        /// <summary>
        /// Gets the remote branch which is connected to this local one, or null if there is none.
        /// </summary>
        public override Branch TrackedBranch
        {
            get { return null; }
        }
    }
}
