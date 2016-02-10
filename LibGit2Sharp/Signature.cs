using System;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A signature
    /// </summary>
    public sealed class Signature : IEquatable<Signature>
    {
        private readonly DateTimeOffset when;
        private readonly string name;
        private readonly string email;

        private static readonly LambdaEqualityHelper<Signature> equalityHelper =
            new LambdaEqualityHelper<Signature>(x => x.Name, x => x.Email, x => x.When);

        internal Signature(IntPtr signaturePtr)
        {
            var handle = signaturePtr.MarshalAs<GitSignature>();

            name = LaxUtf8Marshaler.FromNative(handle.Name);
            email = LaxUtf8Marshaler.FromNative(handle.Email);
            when = Epoch.ToDateTimeOffset(handle.When.Time, handle.When.Offset);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Signature"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="email">The email.</param>
        /// <param name="when">The when.</param>
        public Signature(string name, string email, DateTimeOffset when)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(email, "email");
            Ensure.ArgumentDoesNotContainZeroByte(name, "name");
            Ensure.ArgumentDoesNotContainZeroByte(email, "email");

            this.name = name;
            this.email = email;
            this.when = when;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Signature"/> class.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <param name="when">The when.</param>
        public Signature(Identity identity, DateTimeOffset when)
        {
            Ensure.ArgumentNotNull(identity, "identity");

            this.name = identity.Name;
            this.email = identity.Email;
            this.when = when;
        }

        internal SignatureSafeHandle BuildHandle()
        {
            return Proxy.git_signature_new(name, email, when);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Gets the email.
        /// </summary>
        public string Email
        {
            get { return email; }
        }

        /// <summary>
        /// Gets the date when this signature happened.
        /// </summary>
        public DateTimeOffset When
        {
            get { return when; }
        }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Signature"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Signature"/>.</param>
        /// <returns>True if the specified <see cref="Object"/> is equal to the current <see cref="Signature"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Signature);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Signature"/> is equal to the current <see cref="Signature"/>.
        /// </summary>
        /// <param name="other">The <see cref="Signature"/> to compare with the current <see cref="Signature"/>.</param>
        /// <returns>True if the specified <see cref="Signature"/> is equal to the current <see cref="Signature"/>; otherwise, false.</returns>
        public bool Equals(Signature other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        /// Tests if two <see cref="Signature"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="Signature"/> to compare.</param>
        /// <param name="right">Second <see cref="Signature"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Signature left, Signature right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="Signature"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="Signature"/> to compare.</param>
        /// <param name="right">Second <see cref="Signature"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Signature left, Signature right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Returns "<see cref="Name"/> &lt;<see cref="Email"/>&gt;" for the current <see cref="Signature"/>.
        /// </summary>
        /// <returns>The <see cref="Name"/> and <see cref="Email"/> of the current <see cref="Signature"/>.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} <{1}>", Name, Email);
        }
    }

    internal static class SignatureHelpers
    {
        /// <summary>
        /// Build the handle for the Signature, or return a handle
        /// to an empty signature.
        /// </summary>
        /// <param name="signature"></param>
        /// <returns></returns>
        public static SignatureSafeHandle SafeBuildHandle(this Signature signature)
        {
            if (signature == null)
            {
                return new SignatureSafeHandle();
            }

            return signature.BuildHandle();
        }
    }
}
