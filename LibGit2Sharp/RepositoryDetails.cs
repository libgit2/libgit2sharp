using System.IO;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides high level information about a repository.
    /// </summary>
    public class RepositoryDetails
    {
        private readonly Repository repo;

        internal RepositoryDetails(Repository repo, string posixPath, string posixWorkingDirectoryPath, bool isBare)
        {
            this.repo = repo;
            Path = PosixPathHelper.ToNative(posixPath);
            IsBare = isBare;
            WorkingDirectory = PosixPathHelper.ToNative(posixWorkingDirectoryPath);
        }

        /// <summary>
        ///   Gets the normalized path to the git repository.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        ///   Gets the normalized path to the working directory.
        /// <para>
        ///   Is the repository is bare, null is returned.
        /// </para>
        /// </summary>
        public string WorkingDirectory { get; private set; }

        /// <summary>
        ///   Indicates whether the repository has a working directory.
        /// </summary>
        public bool IsBare { get; private set; }

        /// <summary>
        ///   Indicates whether the Head points to an arbitrary commit instead of the tip of a local banch.
        /// </summary>
        public bool IsHeadDetached { get { return repo.Refs.Head is DirectReference; } }
    }
}