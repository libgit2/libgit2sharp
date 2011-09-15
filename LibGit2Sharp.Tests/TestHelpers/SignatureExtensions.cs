using System;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public static class SignatureExtensions
    {
        public static Signature TimeShift(this Signature signature, TimeSpan shift)
        {
            return new Signature(signature.Name, signature.Email, signature.When.Add(shift));
        }
    }
}
