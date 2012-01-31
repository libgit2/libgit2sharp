using System;
using System.Runtime.InteropServices;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A signature
    /// </summary>
    public class Signature
    {
        private readonly GitSignature handle = new GitSignature();
        private DateTimeOffset? when;
        private string name;
        private string email;

        internal Signature(IntPtr signaturePtr, bool ownedByRepo = true)
        {
            var marshaler = new Utf8Marshaler();
            Marshal.PtrToStructure(signaturePtr, handle);

            // XXX: This is unbelievably hacky, but I can't get the 
            // Utf8Marshaller to work properly.
            name = (string)marshaler.MarshalNativeToManaged(handle.Name);
            email = (string)marshaler.MarshalNativeToManaged(handle.Email);
            
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
            : this(CreateSignature(name, email, when), false)
        {
        }

        private static IntPtr CreateSignature(string name, string email, DateTimeOffset when)
        {
            IntPtr signature;
            int result = NativeMethods.git_signature_new(out signature, name, email, when.ToSecondsSinceEpoch(), (int)when.Offset.TotalMinutes);
            Ensure.Success(result);

            return signature;
        }

        internal GitSignature Handle
        {
            get { return handle; }
        }

        /// <summary>
        ///   Gets the name.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        ///   Gets the email.
        /// </summary>
        public string Email
        {
            get { return email; }
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
                    when = Epoch.ToDateTimeOffset(handle.When.Time, handle.When.Offset);
                }
                return when.Value;
            }
        }
    }
}
