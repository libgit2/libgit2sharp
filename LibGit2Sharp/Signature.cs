using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A signature
    /// </summary>
    public class Signature
    {
        private readonly DateTimeOffset when;
        private readonly string name;
        private readonly string email;

        internal Signature(IntPtr signaturePtr)
        {
            var handle = new GitSignature();
            Marshal.PtrToStructure(signaturePtr, handle);

            name = Utf8Marshaler.FromNative(handle.Name);
            email = Utf8Marshaler.FromNative(handle.Email);
            when = Epoch.ToDateTimeOffset(handle.When.Time, handle.When.Offset);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Signature" /> class.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "email">The email.</param>
        /// <param name = "when">The when.</param>
        public Signature(string name, string email, DateTimeOffset when)
        {
            this.name = name;
            this.email = email;
            this.when = when;
        }

        internal SignatureSafeHandle BuildHandle()
        {
            return Proxy.git_signature_new(name, email, when);
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
            get { return when; }
        }
    }
}
