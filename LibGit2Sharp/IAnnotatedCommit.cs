using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Interface to retrieve an annotated commit from another type
    /// </summary>
    public interface IAnnotatedCommit
    {
        /// <summary>
        /// Retrieve an annotated commit from this object
        /// </summary>
        AnnotatedCommit GetAnnotatedCommit();
    }
}

