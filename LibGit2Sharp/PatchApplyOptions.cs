namespace LibGit2Sharp
{
    /// <summary>
    /// The options to be used for patch application.
    /// </summary>
    public sealed class PatchApplyOptions
    {
        /// <summary>
        /// The location to apply (workdir, index or both)
        /// </summary>
        public PatchApplyLocation Location { get; set; }
    }

    /// <summary>
    /// Possible application locations for applying a patch.
    /// </summary>
    public enum PatchApplyLocation
    {
        /// <summary>
        /// Apply the patch to the workdir, leaving the index untouched.
        /// This is the equivalent of `git apply` with no location argument.
        /// </summary>
        Workdir = 0,

        /// <summary>
        /// Apply the patch to the index, leaving the working directory
        /// untouched.  This is the equivalent of `git apply --cached`.
        /// </summary>
        Index = 1,

        /// <summary>
        /// Apply the patch to both the working directory and the index.
        /// This is the equivalent of `git apply --index`.
        /// </summary>
        Both = 2
    }
}
