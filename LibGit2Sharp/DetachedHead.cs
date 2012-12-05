namespace LibGit2Sharp
{
    internal class DetachedHead : Branch
    {
        internal DetachedHead(Repository repo, Reference reference)
            : base(repo, reference, "(no branch)")
        {
        }

        protected override string Shorten(string branchName)
        {
            return branchName;
        }

        /// <summary>
        ///   Detached Head cannot be a tracking branch.
        /// </summary>
        public override bool IsTracking
        {
            get
            {
                return false;
            }
        }
    }
}