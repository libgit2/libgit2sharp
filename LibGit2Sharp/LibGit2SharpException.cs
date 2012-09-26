using System;
using System.Globalization;
using System.Runtime.Serialization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The exception that is thrown when an error occurs during application execution.
    /// </summary>
    [Obsolete("This type will be removed in the next release. Please use LibGit2SharpException instead.")]
    public class LibGit2Exception : LibGit2SharpException
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "LibGit2Exception" /> class.
        /// </summary>
        public LibGit2Exception()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "LibGit2Exception" /> class with a specified error message.
        /// </summary>
        /// <param name = "message">A message that describes the error. </param>
        public LibGit2Exception(string message)
            : base(message)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "LibGit2Exception" /> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name = "message">The error message that explains the reason for the exception. </param>
        /// <param name = "innerException">The exception that is the cause of the current exception. If the <paramref name = "innerException" /> parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public LibGit2Exception(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    ///   The exception that is thrown when an error occurs during application execution.
    /// </summary>
    [Serializable]
    public class LibGit2SharpException : Exception
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "LibGit2SharpException" /> class.
        /// </summary>
        public LibGit2SharpException()
        {
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

        /// <summary>
        ///   The libgit2 error code associated with the exception
        /// </summary>
        public GitErrorCode Code { get; protected set; }

        /// <summary>
        ///   The libgit2 component that threw the error
        /// </summary>
        public GitErrorCategory Category { get; protected set; }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "LibGit2SharpException" /> class with a serialized data.
        /// </summary>
        /// <param name = "info">The <see cref="SerializationInfo "/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name = "context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected LibGit2SharpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        internal LibGit2SharpException(string message, GitErrorCode code, GitErrorCategory category) : this(FormatMessage(message, code, category))
        {
            Code = code;
            Category = category;

            // NB: Not strictly needed now, but leave this for backwards compat
            Data.Add("libgit2.code", code);
            Data.Add("libgit2.category", category);
        }

        private static string FormatMessage(string message, GitErrorCode code, GitErrorCategory category)
        {
            return String.Format(CultureInfo.InvariantCulture, "An error was raised by libgit2. Category = {0} ({1}).{2}{3}",
                          category,
                          code,
                          Environment.NewLine,
                          message);
        }
    }
}
