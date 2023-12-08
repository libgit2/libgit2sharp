namespace LibGit2Sharp
{
    /// <summary>
    /// What to do with a delta.
    /// </summary>
    public enum DeltaApplyStatus
    {
        /// <summary>
        /// Apply the delta.
        /// </summary>
        Apply,

        /// <summary>
        /// Do not apply the delta and abort.
        /// </summary>
        Abort,

        /// <summary>
        /// Do not apply the delta and continue.
        /// </summary>
        Skip
    }
}
