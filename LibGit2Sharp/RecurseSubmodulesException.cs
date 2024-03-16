using System;
#if NETFRAMEWORK
using System.Runtime.Serialization;
#endif

namespace LibGit2Sharp
{
    /// <summary>
    /// The exception that is thrown when an error is encountered while recursing
    /// through submodules. The inner exception contains the exception that was
    /// initially thrown while operating on the submodule.
    /// </summary>
#if NETFRAMEWORK
    [Serializable]
#endif
    public class RecurseSubmodulesException : LibGit2SharpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecurseSubmodulesException"/> class.
        /// </summary>
        public RecurseSubmodulesException()
        { }

        /// <summary>
        /// The path to the initial repository the operation was run on.
        /// </summary>
        public virtual string InitialRepositoryPath { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurseSubmodulesException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the <paramref name="innerException"/> parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        /// <param name="initialRepositoryPath">The path to the initial repository the operation was performed on.</param>
        public RecurseSubmodulesException(string message, Exception innerException, string initialRepositoryPath)
            : base(message, innerException)
        {
            InitialRepositoryPath = initialRepositoryPath;
        }

#if NETFRAMEWORK
        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2Sharp.RecurseSubmodulesException"/> class with a serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected RecurseSubmodulesException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
#endif
    }
}
