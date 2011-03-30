using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A signature
    /// </summary>
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

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Signature" /> class.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "email">The email.</param>
        /// <param name = "when">The when.</param>
        public Signature(string name, string email, DateTimeOffset when)
            : this(NativeMethods.git_signature_new(name, email, when.ToSecondsSinceEpoch(), (int) when.Offset.TotalMinutes), false)
        {
        }

        /// <summary>
        ///   Gets the name.
        /// </summary>
        public string Name
        {
            get { return sig.Name; }
        }

        /// <summary>
        ///   Gets the email.
        /// </summary>
        public string Email
        {
            get { return sig.Email; }
        }

        /// <summary>
        ///   Gets the date when this signature happened.
        /// </summary>
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