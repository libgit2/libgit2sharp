using System;

namespace LibGit2Sharp
{
    public class CyclicReferenceException : Exception
    {
        public CyclicReferenceException(string reference) : base($"Detected cyclic reference on '{reference}'")
        {
        }
    }
}
