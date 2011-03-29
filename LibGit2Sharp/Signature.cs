using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp
{
    public class Signature
    {
        private readonly GitSignature sig = new GitSignature();
        private DateTimeOffset? when;

        internal Signature(IntPtr signaturePtr, bool ownedByRepo = true)
        {
            Marshal.PtrToStructure(signaturePtr, sig);
            if (!ownedByRepo)
            {
                NativeMethods.git_signature_free(signaturePtr);
            }
        }

        public Signature(string name, string email, DateTimeOffset when)
            : this(NativeMethods.git_signature_new(name, email, when.ToSecondsSinceEpoch(), (int) when.Offset.TotalMinutes), false)
        {
        }

        public string Name
        {
            get { return sig.Name; }
        }

        public string Email
        {
            get { return sig.Email; }
        }

        public DateTimeOffset When
        {
            get
            {
                if (when == null)
                {
                    when = Epoch.ToDateTimeOffset(sig.When.Time, sig.When.Offset);
                }
                return when.Value;
            }
        }
    }
}