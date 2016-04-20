namespace LibGit2Sharp
{
    internal class VoidReference : Reference
    {
        internal VoidReference(IRepository repo, string canonicalName)
            : base(repo, canonicalName, null)
        { }

        public override DirectReference ResolveToDirectReference()
        {
            return null;
        }
    }
}
