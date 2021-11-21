namespace LibGit2Sharp
{
    public class CyclicReferenceException : LibGit2SharpException
    {
        public CyclicReferenceException(string reference) : base($"Detected cyclic reference on '{reference}'")
        {
        }

        protected CyclicReferenceException() {}
    }
}
