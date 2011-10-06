namespace LibGit2Sharp
{
    internal class DetachedHead : Branch
    {
        internal DetachedHead(Repository repo, Reference reference)
            : base(repo, reference, "(no branch)")
        {
        }

        /// <summary>
        ///   Gets a value indicating whether this instance is current branch (HEAD) in the repository.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is current branch; otherwise, <c>false</c>.
        /// </value>
        public override bool IsCurrentRepositoryHead
        {
            get
            {
                return repo.Head == this;
            }
        }

        protected override string Shorten(string branchName)
        {
            return branchName;
        }
    }
}