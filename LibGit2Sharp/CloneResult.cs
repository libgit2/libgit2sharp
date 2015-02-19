using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// Class to report the result of a clone.
    /// </summary>
    public class CloneResult
    {
        /// <summary>
        /// Needed for mocking.
        /// </summary>
        protected CloneResult()
        { }

        /// <summary>
        /// CloneResult constructor.
        /// </summary>
        internal CloneResult(string repoPath, string workdirPath, Exception recursiveException)
        {
            WorkingDirectory = workdirPath;
            RepoPath = repoPath;
            RecursiveException = recursiveException;
        }

        /// <summary>
        /// Gets the path to the working directory.
        /// <para>
        ///   If the repository is bare, null is returned.
        /// </para>
        /// </summary>
        public virtual string WorkingDirectory { get; private set; }

        /// <summary>
        /// Gets the path to the git repository.
        /// </summary>
        public virtual string RepoPath { get; private set; }

        /// <summary>
        /// The exception encountered while recursively cloning submodules (if any).
        /// </summary>
        public virtual Exception RecursiveException { get; set; }
    }
}
