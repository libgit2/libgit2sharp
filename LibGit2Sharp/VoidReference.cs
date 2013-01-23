namespace LibGit2Sharp
{
    internal class VoidReference : Reference
    {
        internal VoidReference(string canonicalName)
            : base(canonicalName, null)
        { }

        public override DirectReference ResolveToDirectReference()
        {
            return null;
        }
    }
}
