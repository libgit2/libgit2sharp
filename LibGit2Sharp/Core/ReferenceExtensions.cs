namespace LibGit2Sharp.Core
{
    internal static class ReferenceExtensions
    {
        private static bool IsPeelable(this Reference reference)
        {
            return reference is DirectReference || (reference is SymbolicReference && ((SymbolicReference)reference).Target != null);
        }

        public static ObjectId PeelToTargetObjectId(this Reference reference)
        {
            if (!reference.IsPeelable())
            {
                return null;
            }

            string sha = reference.ResolveToDirectReference().TargetIdentifier;

            return new ObjectId(sha);
        }
    }
}
