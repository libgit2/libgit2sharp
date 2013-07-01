namespace LibGit2Sharp
{
    /// <summary>
    /// Extensions related to submodules
    /// </summary>
    public static class SubmoduleExtensions
    {
        private const SubmoduleStatus UnmodifiedMask = ~(SubmoduleStatus.InConfig | SubmoduleStatus.InHead | SubmoduleStatus.InIndex | SubmoduleStatus.InWorkDir);
        private const SubmoduleStatus WorkDirDirtyMask = SubmoduleStatus.WorkDirFilesIndexDirty | SubmoduleStatus.WorkDirFilesModified | SubmoduleStatus.WorkDirFilesUntracked;

        /// <summary>
        /// The submodule is unmodified.
        /// </summary>
        public static bool IsUnmodified(this SubmoduleStatus @this)
        {
            return (@this & UnmodifiedMask) == SubmoduleStatus.Unmodified;
        }

        /// <summary>
        /// The submodule working directory is dirty.
        /// </summary>
        public static bool IsWorkingDirectoryDirty(this SubmoduleStatus @this)
        {
            return (@this & WorkDirDirtyMask) != SubmoduleStatus.Unmodified;
        }
    }
}
