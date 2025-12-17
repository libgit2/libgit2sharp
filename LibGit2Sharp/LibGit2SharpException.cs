using System;
using System.Globalization;
#if NETFRAMEWORK
using System.Runtime.Serialization;
#endif

namespace LibGit2Sharp
{
    /// <summary>
    /// The exception that is thrown when an error occurs during application execution.
    /// </summary>
#if NETFRAMEWORK
    [Serializable]
#endif
    public class LibGit2SharpException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2SharpException"/> class.
        /// </summary>
        public LibGit2SharpException()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2SharpException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        public LibGit2SharpException(string message)
            : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2SharpException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the <paramref name="innerException"/> parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public LibGit2SharpException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2SharpException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="format">A composite format string for use in <see cref="string.Format(IFormatProvider, string, object[])"/>.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public LibGit2SharpException(string format, params object[] args)
            : base(string.Format(CultureInfo.InvariantCulture, format, args))
        {
        }

#if NETFRAMEWORK
        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2SharpException"/> class with a serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected LibGit2SharpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
#endif
    }
}
