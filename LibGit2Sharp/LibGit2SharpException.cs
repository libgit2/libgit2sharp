using System;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The exception that is thrown when an error occurs during application execution.
    /// </summary>
    public class LibGit2SharpException : Exception
    {
        readonly bool isLibraryError;

        /// <summary>
        /// The error code originally returned by libgit2.
        /// </summary>
        public GitErrorCode Code { get; private set; }

        /// <summary>
        /// The category of error raised by libgit2.
        /// </summary>
        public GitErrorCategory Category { get; private set; }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "LibGit2SharpException" /> class.
        /// </summary>
        public LibGit2SharpException()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "LibGit2SharpException" /> class.
        /// </summary>
        public LibGit2SharpException(GitErrorCode code, GitErrorCategory category, string message) : base(message)
        {
            Code = code;
            Category = category;
            isLibraryError = true;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "LibGit2SharpException" /> class with a specified error message.
        /// </summary>
        /// <param name = "message">A message that describes the error. </param>
        public LibGit2SharpException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "LibGit2SharpException" /> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name = "message">The error message that explains the reason for the exception. </param>
        /// <param name = "innerException">The exception that is the cause of the current exception. If the <paramref name = "innerException" /> parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public LibGit2SharpException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public override string ToString()
        {
            return isLibraryError
                ? String.Format(CultureInfo.InvariantCulture, "An error was raised by libgit2. Class = {0} ({1}).{2}{3}",
                    Category,
                    Code,
                    Environment.NewLine,
                    Message)
                : base.ToString();
        }
    }
}
