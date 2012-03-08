namespace LibGit2Sharp
{
    public interface IRepositoryInformation
    {
        /// <summary>
        ///   Gets the normalized path to the git repository.
        /// </summary>
        string Path { get; }

        /// <summary>
        ///   Gets the normalized path to the working directory.
        ///   <para>
        ///     Is the repository is bare, null is returned.
        ///   </para>
        /// </summary>
        string WorkingDirectory { get; }

        /// <summary>
        ///   Indicates whether the repository has a working directory.
        /// </summary>
        bool IsBare { get; }

        /// <summary>
        ///   Gets a value indicating whether this repository is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this repository is empty; otherwise, <c>false</c>.
        /// </value>
        bool IsEmpty { get; }

        /// <summary>
        ///   Indicates whether the Head points to an arbitrary commit instead of the tip of a local banch.
        /// </summary>
        bool IsHeadDetached { get; }
    }
}