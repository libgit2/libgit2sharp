using System;

namespace LibGit2Sharp.Tests.TestHelpers
{
    class SkipException : Exception
    {
        public SkipException(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; set; }
    }
}