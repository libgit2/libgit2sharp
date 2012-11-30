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
        ///   Determines if this local branch is connected to a remote one.
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
