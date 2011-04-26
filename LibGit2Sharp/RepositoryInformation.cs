using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides high level information about a repository.
    /// </summary>
    public class RepositoryInformation
    {
        private readonly Repository repo;

        internal RepositoryInformation(Repository repo, string posixPath, string posixWorkingDirectoryPath, bool isBare)
        {
            this.repo = repo;
            Path = PosixPathHelper.ToNative(posixPath);
            IsBare = isBare;
            WorkingDirectory = PosixPathHelper.ToNative(posixWorkingDirectoryPath);
            IsEmpty = NativeMethods.git_repository_is_empty(repo.Handle);
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
        public bool IsEmpty { get; private set; }

        /// <summary>
        ///   Indicates whether the Head points to an arbitrary commit instead of the tip of a local banch.
        /// </summary>
        public bool IsHeadDetached
        {
            get
            {
                if (repo.Info.IsEmpty) return false; // Detached HEAD doesn't mean anything for an empty repo, just return false
                return repo.Refs.Head is DirectReference;
            }
        }
    }
}