namespace LibGit2Sharp
{
    public class DetachedHead : Branch
    {
        public DetachedHead(Repository repo, Reference reference)
            : base(repo, reference, "(no branch)")
        {
        }

        protected override string Shorten(string branchName)
        {
            return branchName;
        }
    }
}
