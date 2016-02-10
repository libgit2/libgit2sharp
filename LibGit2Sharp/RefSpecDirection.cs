namespace LibGit2Sharp
{
    /// <summary>
    /// Indicates whether a refspec is a push refspec or a fetch refspec
    /// </summary>
    public enum RefSpecDirection
    {
        /// <summary>
        /// Indicates that the refspec is a fetch refspec
        /// </summary>
        Fetch,

        /// <summary>
        /// Indicates that the refspec is a push refspec
        /// </summary>
        Push
    }
}
