using System;
using System.Globalization;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Interface for classes that can generate a signature.
    /// </summary>
    public interface ISignature
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the Email.
        /// </summary>
        string Email { get; }

        /// <summary>
        /// Gets the date for this signature.
        /// </summary>
        DateTimeOffset When { get; }
    }

    /// <summary>
    /// A signature
    /// </summary>
    public sealed class Signature : IEquatable<Signature>, ISignature
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

        internal Signature(SignatureSafeHandle signaturePtr)
        {
            var gitSignature = signaturePtr.MarshalAsGitSignature();

            name = LaxUtf8Marshaler.FromNative(gitSignature.Name);
            email = LaxUtf8Marshaler.FromNative(gitSignature.Email);
            when = Epoch.ToDateTimeOffset(gitSignature.When.Time, gitSignature.When.Offset);
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

    /// <summary>
    /// Returns a signature with the current time
    /// </summary>
    public class CurrentTimeSignature : ISignature
    {
        private string name;
        private string email;

        /// <summary>
        /// This generator will generate new signatures with the  the name and
        /// email from the passed in signature, but will use current time
        /// stamps.
        /// </summary>
        /// <param name="signature"></param>
        public CurrentTimeSignature(Signature signature)
        {
            name = signature.Name;
            email = signature.Email;
        }

        public CurrentTimeSignature(string name, string email)
        {
            this.name = name;
            this.email = email;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Gets the Email.
        /// </summary>
        public string Email
        {
            get
            {
                return email;
            }
        }

        /// <summary>
        /// Gets the current time.
        /// </summary>
        public DateTimeOffset When
        {
            get
            {
                return DateTimeOffset.Now;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ISignatureExtensions
    {
        internal static SignatureSafeHandle BuildHandle(this ISignature signature)
        {
            return Proxy.git_signature_new(signature.Name, signature.Email, signature.When);
        }
    }
}
