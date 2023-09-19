using System;
using System.Runtime.Serialization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// The exception that is thrown when a provided specification is bad. This
    /// can happen if the provided specification is syntactically incorrect, or
    /// if the spec refers to an object of an incorrect type (e.g. asking to
    /// create a branch from a blob, or peeling a blob to a commit).
    /// </summary>
    [Serializable]
    public class InvalidSpecificationException : NativeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSpecificationException"/> class.
        /// </summary>
        public InvalidSpecificationException()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSpecificationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        public InvalidSpecificationException(string message)
            : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSpecificationException"/> class with a specified error message.
        /// </summary>
        /// <param name="format">A composite format string for use in <see cref="String.Format(IFormatProvider, string, object[])"/>.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public InvalidSpecificationException(string format, params object[] args)
            : base(format, args)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSpecificationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the <paramref name="innerException"/> parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public InvalidSpecificationException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSpecificationException"/> class with a serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected InvalidSpecificationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        internal InvalidSpecificationException(string message, GitErrorCategory category)
            : base(message, category)
        { }

        internal override GitErrorCode ErrorCode
        {
            get
            {
                return GitErrorCode.InvalidSpecification;
            }
        }
    }
}
