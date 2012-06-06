namespace LibGit2Sharp
{
    public interface IBranch
    {
        /// <summary>
        ///   Gets the <see cref = "TreeEntry" /> pointed at by the <paramref name = "relativePath" /> in the <see cref = "Tip" />.
        /// </summary>
        /// <param name = "relativePath">The relative path to the <see cref = "TreeEntry" /> from the <see cref = "Tip" /> working directory.</param>
        /// <returns><c>null</c> if nothing has been found, the <see cref = "TreeEntry" /> otherwise.</returns>
        TreeEntry this[string relativePath] { get; }

        /// <summary>
        ///   Gets a value indicating whether this instance is a remote.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is remote; otherwise, <c>false</c>.
        /// </value>
        bool IsRemote { get; }

        /// <summary>
        ///   Gets the remote branch which is connected to this local one.
        /// </summary>
        IBranch TrackedBranch { get; }

        /// <summary>
        ///   Determines if this local branch is connected to a remote one.
        /// </summary>
        bool IsTracking { get; }

        /// <summary>
        ///   Gets the number of commits, starting from the <see cref="Tip"/>, that have been performed on this local branch and aren't known from the remote one.
        /// </summary>
        int AheadBy { get; }

        /// <summary>
        ///   Gets the number of commits that exist in the remote branch, on top of <see cref="Tip"/>, and aren't known from the local one.
        /// </summary>
        int BehindBy { get; }

        /// <summary>
        ///   Gets a value indicating whether this instance is current branch (HEAD) in the repository.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is the current branch; otherwise, <c>false</c>.
        /// </value>
        bool IsCurrentRepositoryHead { get; }

        /// <summary>
        ///   Gets the commit id that this branch points to.
        /// </summary>
        Commit Tip { get; }

        /// <summary>
        ///   Gets the commits on this branch. (Starts walking from the References's target).
        /// </summary>
        ICommitLog Commits { get; }

        /// <summary>
        ///   Gets the full name of this reference.
        /// </summary>
        string CanonicalName { get; }

        /// <summary>
        ///   Gets the name of this reference.
        /// </summary>
        string Name { get; }
    }
}