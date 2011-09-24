using System;

namespace LibGit2Sharp
{
    public class LibGit2Exception : Exception
    {
        public LibGit2Exception()
        {
        }

        public LibGit2Exception(string message)
            : base(message)
        {
        }

        public LibGit2Exception(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
