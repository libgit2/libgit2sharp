namespace LibGit2Sharp
{
    /// <summary>
    ///   Optional parameters that can be defined when opening or creating a <see cref = "Repository" />
    /// </summary>
    public class RepositoryOptions
    {
        /// <summary>
        ///   Gets or sets a value indicating whether to create a new git repository if one does not exist at the specified path.
        /// </summary>
        /// <value>
        ///   <c>true</c> if a repository should be created (if needed); otherwise, <c>false</c>.
        /// </value>
        public bool CreateIfNeeded { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether this is a bare git repository or whether a bare repository should be created.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this is a bare repository; otherwise, <c>false</c>.
        /// </value>
        public bool IsBareRepository { get; set; }

        public bool ImmediatelyHydrateObject { get; set; }
    }
}