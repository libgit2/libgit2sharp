namespace LibGit2Sharp
{
    /// <summary>
    ///   A DirectReference points directly to a <see cref = "GitObject" />
    /// </summary>
    public class DirectReference : Reference
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "DirectReference" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        public DirectReference(Repository repo) : base(repo)
        {
        }

        /// <summary>
        ///   Gets the target of this <see cref = "DirectReference" />
        /// </summary>
        public GitObject Target { get; internal set; }
    }
}