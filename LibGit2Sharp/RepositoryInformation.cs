using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides high level information about a repository.
    /// </summary>
    public class RepositoryInformation
    {
        private readonly Repository repo;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected RepositoryInformation()
        { }

        internal RepositoryInformation(Repository repo, bool isBare)
        {
            this.repo = repo;
            IsBare = isBare;

            FilePath path = Proxy.git_repository_path(repo.Handle);
            FilePath workingDirectoryPath = Proxy.git_repository_workdir(repo.Handle);

            Path = path.Native;
            WorkingDirectory = workingDirectoryPath == null ? null : workingDirectoryPath.Native;
        }

        /// <summary>
        ///   Gets the normalized path to the git repository.
        /// </summary>
        public virtual string Path { get; private set; }

        /// <summary>
        ///   Gets the normalized path to the working directory.
        ///   <para>
        ///     Is the repository is bare, null is returned.
        ///   </para>
        /// </summary>
        public virtual string WorkingDirectory { get; private set; }

        /// <summary>
        ///   Indicates whether the repository has a working directory.
        /// </summary>
        public virtual bool IsBare { get; private set; }

        /// <summary>
        ///   Gets a value indicating whether this repository is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this repository is empty; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsEmpty
        {
            get { return Proxy.git_repository_is_empty(repo.Handle); }
        }

        /// <summary>
        ///   Indicates whether the Head points to an arbitrary commit instead of the tip of a local branch.
        /// </summary>
        public virtual bool IsHeadDetached
        {
            get { return Proxy.git_repository_head_detached(repo.Handle); }
        }

        /// <summary>
        ///   Indicates whether the Head points to a reference which doesn't exist.
        /// </summary>
        public virtual bool IsHeadOrphaned
        {
            get { return Proxy.git_repository_head_orphan(repo.Handle); }
        }

        /// <summary>
        ///   The pending interactive operation.
        /// </summary>
        public virtual CurrentOperation CurrentOperation
        {
            get { return Proxy.git_repository_state(repo.Handle); }
        }
    }
}
