using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides high level information about a repository.
    /// </summary>
    public class RepositoryInformation
    {
        private readonly Repository repo;

        /// <summary>
        /// Needed for mocking purposes.
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
            IsShallow = Proxy.git_repository_is_shallow(repo.Handle);
        }

        /// <summary>
        /// Gets the normalized path to the git repository.
        /// </summary>
        public virtual string Path { get; private set; }

        /// <summary>
        /// Gets the normalized path to the working directory.
        /// <para>
        ///   Is the repository is bare, null is returned.
        /// </para>
        /// </summary>
        public virtual string WorkingDirectory { get; private set; }

        /// <summary>
        /// Indicates whether the repository has a working directory.
        /// </summary>
        public virtual bool IsBare { get; private set; }

        /// <summary>
        /// Indicates whether the repository is shallow (the result of `git clone --depth ...`)
        /// </summary>
        public virtual bool IsShallow { get; private set; }

        /// <summary>
        /// Indicates whether the Head points to an arbitrary commit instead of the tip of a local branch.
        /// </summary>
        public virtual bool IsHeadDetached
        {
            get { return Proxy.git_repository_head_detached(repo.Handle); }
        }

        /// <summary>
        /// Indicates whether the Head points to a reference which doesn't exist.
        /// </summary>
        public virtual bool IsHeadUnborn
        {
            get { return Proxy.git_repository_head_unborn(repo.Handle); }
        }

        /// <summary>
        /// The pending interactive operation.
        /// </summary>
        public virtual CurrentOperation CurrentOperation
        {
            get { return Proxy.git_repository_state(repo.Handle); }
        }

        /// <summary>
        /// The message for a pending interactive operation.
        /// </summary>
        public virtual string Message
        {
            get { return Proxy.git_repository_message(repo.Handle); }
        }
    }
}
