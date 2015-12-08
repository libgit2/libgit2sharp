using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// The exception that is thrown when a checkout cannot be performed
    /// because of a conflicting change staged in the index, or unstaged
    /// in the working directory.
    /// </summary>
    [Serializable]
    public class CheckoutConflictException : MergeConflictException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2Sharp.CheckoutConflictException"/> class.
        /// </summary>
        public CheckoutConflictException()
        { }

        internal CheckoutConflictException(string message, GitErrorCode code, GitErrorCategory category)
            : base(message, code, category)
        { }
    }
}
