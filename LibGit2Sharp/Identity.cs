using LibGit2Sharp.Core;

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
            Ensure.ArgumentDoesNotContainZeroByte(name, "email");

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
    }
}
