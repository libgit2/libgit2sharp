﻿using System;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The exception that is thrown when an error occurs during application execution.
    /// </summary>
    public class LibGit2Exception : Exception
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
        public LibGit2Exception(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
