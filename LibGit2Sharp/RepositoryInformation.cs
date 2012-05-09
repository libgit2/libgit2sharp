using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides high level information about a repository.
    /// </summary>
    public class RepositoryInformation : IRepositoryInformation
    {
        private readonly Repository repo;

        internal RepositoryInformation(Repository repo, bool isBare)
        {
            this.repo = repo;
            IsBare = isBare;

            FilePath path = NativeMethods.git_repository_path(repo.Handle);
            FilePath workingDirectoryPath = NativeMethods.git_repository_workdir(repo.Handle);

            Path = path.Native;
            WorkingDirectory = workingDirectoryPath == null ? null : workingDirectoryPath.Native;
        }

        /// <summary>
        ///   Gets the normalized path to the git repository.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        ///   Gets the normalized path to the working directory.
        ///   <para>
        ///     Is the repository is bare, null is returned.
        ///   </para>
        /// </summary>
        public string WorkingDirectory { get; private set; }

        /// <summary>
        ///   Indicates whether the repository has a working directory.
        /// </summary>
        public bool IsBare { get; private set; }

        /// <summary>
        ///   Gets a value indicating whether this repository is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this repository is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty
        {
            get { return NativeMethods.RepositoryStateChecker(repo.Handle, NativeMethods.git_repository_is_empty); }
        }

        /// <summary>
        ///   Indicates whether the Head points to an arbitrary commit instead of the tip of a local branch.
        /// </summary>
        public bool IsHeadDetached
        {
            get { return NativeMethods.RepositoryStateChecker(repo.Handle, NativeMethods.git_repository_head_detached); }
        }
    }
}
