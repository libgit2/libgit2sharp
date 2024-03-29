﻿using System;
#if NETFRAMEWORK
using System.Runtime.Serialization;
#endif

namespace LibGit2Sharp
{
    /// <summary>
    /// The exception that is thrown when the ref to merge with was as part of a pull operation not fetched.
    /// </summary>
#if NETFRAMEWORK
    [Serializable]
#endif
    public class MergeFetchHeadNotFoundException : NotFoundException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MergeFetchHeadNotFoundException"/> class.
        /// </summary>
        public MergeFetchHeadNotFoundException()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MergeFetchHeadNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        public MergeFetchHeadNotFoundException(string message)
            : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MergeFetchHeadNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="format">A composite format string for use in <see cref="string.Format(IFormatProvider, string, object[])"/>.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public MergeFetchHeadNotFoundException(string format, params object[] args)
            : base(format, args)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MergeFetchHeadNotFoundException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the <paramref name="innerException"/> parameter is not a null reference, the current exception is raised in a catch block that handles the inner exception.</param>
        public MergeFetchHeadNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        { }

#if NETFRAMEWORK
        /// <summary>
        /// Initializes a new instance of the <see cref="MergeFetchHeadNotFoundException"/> class with a serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected MergeFetchHeadNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
#endif
    }
}
