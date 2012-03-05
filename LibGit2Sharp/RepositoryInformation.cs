using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides high level information about a repository.
    /// </summary>
    public class RepositoryInformation
    {
        private readonly Repository repo;

        internal RepositoryInformation(Repository repo, bool isBare)
        {
            this.repo = repo;
            IsBare = isBare;

            string posixPath = NativeMethods.git_repository_path(repo.Handle);
            string posixWorkingDirectoryPath = NativeMethods.git_repository_workdir(repo.Handle);

            Path = PosixPathHelper.ToNative(posixPath);
            WorkingDirectory = PosixPathHelper.ToNative(posixWorkingDirectoryPath);
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
            get
            {
                int res = NativeMethods.git_repository_is_empty(repo.Handle);
                Ensure.Success(res, true);

                return (res == 1);
            }
        }

        /// <summary>
        ///   Indicates whether the Head points to an arbitrary commit instead of the tip of a local banch.
        /// </summary>
        public bool IsHeadDetached
        {
            get
            {
                int res = NativeMethods.git_repository_head_detached(repo.Handle);
                Ensure.Success(res, true);

                return (res == 1);
            }
        }
    }
}
