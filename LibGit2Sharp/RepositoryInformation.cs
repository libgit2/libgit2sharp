using System.IO;
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

        /// <summary>
        ///   Gets the pending interactive operation.
        /// </summary>
        public PendingOperation PendingOperation
        {
            get { return DetermineCurrentInteractiveState(); }
        }

        private PendingOperation DetermineCurrentInteractiveState()
        {
            if (!IsHeadDetached)
                return PendingOperation.None;

            if (DirectoryExists("rebase-merge"))
                if (Exists("rebase-merge/interactive"))
                    return PendingOperation.RebaseInteractive;
                else
                    return PendingOperation.Merge;

            if (DirectoryExists("rebase-apply"))
                if (Exists("rebase-apply/rebasing"))
                    return PendingOperation.Rebase;
                else if (Exists("rebase-apply/applying"))
                    return PendingOperation.ApplyMailbox;
                else
                    return PendingOperation.ApplyMailboxOrRebase;

            if (Exists("MERGE_HEAD"))
                return PendingOperation.Merge;

            if (Exists("CHERRY_PICK_HEAD"))
                return PendingOperation.CherryPick;

            if (Exists("BISECT_LOG"))
                return PendingOperation.Bisect;

            return PendingOperation.None;
        }

        private bool DirectoryExists(string relativePath)
        {
            return Directory.Exists(System.IO.Path.Combine(Path, relativePath));
        }

        private bool Exists(string relativePath)
        {
            return File.Exists(System.IO.Path.Combine(Path, relativePath));
        }
    }
}
