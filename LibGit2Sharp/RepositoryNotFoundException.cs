using System;
using System.Runtime.Serialization;

namespace LibGit2Sharp
{
    /// <summary>
    /// The exception that is thrown when a <see cref="Repository"/> is being built with
    /// a path that doesn't point at a valid Git repository or workdir.
    /// </summary>
    [Serializable]
    public class RepositoryNotFoundException : LibGit2SharpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryNotFoundException"/> class.
        /// </summary>
        public RepositoryNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        public RepositoryNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryNotFoundException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the <paramref name="innerException"/> parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public RepositoryNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryNotFoundException"/> class with a serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo "/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected RepositoryNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
