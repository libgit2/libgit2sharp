using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A signature
    /// </summary>
    public class Signature : IEquatable<Signature>
    {
        private readonly DateTimeOffset when;
        private readonly string name;
        private readonly string email;

        private static readonly LambdaEqualityHelper<Signature> equalityHelper =
            new LambdaEqualityHelper<Signature>(x => x.Name, x => x.Email, x => x.When);

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

        /// <summary>
        ///   Determines whether the specified <see cref = "Object" /> is equal to the current <see cref = "Signature" />.
        /// </summary>
        /// <param name = "obj">The <see cref = "Object" /> to compare with the current <see cref = "Signature" />.</param>
        /// <returns>True if the specified <see cref = "Object" /> is equal to the current <see cref = "Signature" />; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Signature);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "Signature" /> is equal to the current <see cref = "Signature" />.
        /// </summary>
        /// <param name = "other">The <see cref = "Signature" /> to compare with the current <see cref = "Signature" />.</param>
        /// <returns>True if the specified <see cref = "Signature" /> is equal to the current <see cref = "Signature" />; otherwise, false.</returns>
        public bool Equals(Signature other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        ///   Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        ///   Tests if two <see cref = "Signature" /> are equal.
        /// </summary>
        /// <param name = "left">First <see cref = "Signature" /> to compare.</param>
        /// <param name = "right">Second <see cref = "Signature" /> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Signature left, Signature right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Tests if two <see cref = "Signature" /> are different.
        /// </summary>
        /// <param name = "left">First <see cref = "Signature" /> to compare.</param>
        /// <param name = "right">Second <see cref = "Signature" /> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Signature left, Signature right)
        {
            return !Equals(left, right);
        }
    }
}
