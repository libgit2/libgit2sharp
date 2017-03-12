namespace LibGit2Sharp
{
    /// <summary>
    /// Specify the kind of committish which will be considered
    /// when trying to identify the closest reference to the described commit.
    /// </summary>
    public enum DescribeStrategy
    {
        /// <summary>
        /// Only consider annotated tags.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Consider both annotated and lightweight tags.
        /// <para>
        ///   This will match every reference under the <code>refs/tags/</code> namespace.
        /// </para>
        /// </summary>
        Tags,

        /// <summary>
        /// Consider annotated and lightweight tags, local and remote tracking branches.
        /// <para>
        ///   This will match every reference under the <code>refs/</code> namespace.
        /// </para>
        /// </summary>
        All,
    }
}
