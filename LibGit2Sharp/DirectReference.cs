namespace LibGit2Sharp
{
    /// <summary>
    ///   A DirectReference points directly to a <see cref = "GitObject" />
    /// </summary>
    public class DirectReference : Reference
    {
        /// <summary>
        ///   Gets the target of this <see cref = "DirectReference" />
        /// </summary>
        public GitObject Target { get; internal set; }
    }
}