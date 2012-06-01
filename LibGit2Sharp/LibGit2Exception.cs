using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The exception that is thrown when an error occurs in libgit2.
    /// </summary>
    public class LibGit2Exception : LibGit2SharpException
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "LibGit2Exception" /> class with a specified error message.
        /// </summary>
        /// <param name="gitError">The libgit2 error</param>
        public LibGit2Exception(GitError gitError)
            : base(gitError.Message)
        {
            GitError = gitError;
        }

        /// <summary>
        /// The underlying libgit2 error
        /// </summary>
        public GitError GitError { get; set; }
    }

    /// <summary>
    ///   The exception that is thrown when an error occurs in libgit2sharp.
    /// </summary>
    public class LibGit2SharpException : Exception
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "LibGit2SharpException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message</param>
        public LibGit2SharpException(string message) : base(message)
        {
        }
    }
}
