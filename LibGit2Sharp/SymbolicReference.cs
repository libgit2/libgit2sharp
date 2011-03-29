namespace LibGit2Sharp
{
    /// <summary>
    ///   A SymbolicReference is a reference that points to another reference
    /// </summary>
    public class SymbolicReference : Reference
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "SymbolicReference" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        public SymbolicReference(Repository repo) : base(repo)
        {
        }

        /// <summary>
        ///   Gets the target of this <see cref = "SymbolicReference" />
        /// </summary>
        public Reference Target { get; internal set; }
    }
}