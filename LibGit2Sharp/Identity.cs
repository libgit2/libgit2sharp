using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Represents the identity used when writing reflog entries.
    /// </summary>
    public sealed class Identity
    {
        private readonly string _name;
        private readonly string _email;

        /// <summary>
        /// Initializes a new instance of the <see cref="Identity"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="email">The email.</param>
        public Identity(string name, string email)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(email, "email");
            Ensure.ArgumentDoesNotContainZeroByte(name, "name");
            Ensure.ArgumentDoesNotContainZeroByte(email, "email");

            _name = name;
            _email = email;
        }

        /// <summary>
        /// Gets the email.
        /// </summary>
        public string Email
        {
            get { return _email; }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        internal SignatureSafeHandle BuildNowSignatureHandle()
        {
            return Proxy.git_signature_now(Name, Email);
        }
    }

    internal static class IdentityHelpers
    {
        /// <summary>
        /// Build the handle for the Indentity with the current time, or return a handle
        /// to an empty signature.
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public static SignatureSafeHandle SafeBuildNowSignatureHandle(this Identity identity)
        {
            if (identity == null)
            {
                return new SignatureSafeHandle();
            }

            return identity.BuildNowSignatureHandle();
        }
    }
}
