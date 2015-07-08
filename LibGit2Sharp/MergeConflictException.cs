using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// The exception that is thrown when a checkout cannot be performed
    /// because of a conflicting change staged in the index, or unstaged
    /// in the working directory.
    /// </summary>
    [Serializable]
    [Obsolete("This type will be removed in the next release. Please use CheckoutConflictException instead.")]
    public class MergeConflictException : CheckoutConflictException
    { }
}
