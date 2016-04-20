namespace LibGit2Sharp
{
    /// <summary>
    /// Flags controlling what files are reported by status.
    /// </summary>
    public enum StatusShowOption
    {
        /// <summary>
        /// Both the index and working directory are examined for changes
        /// </summary>
        IndexAndWorkDir = 0,

        /// <summary>
        /// Only the index is examined for changes
        /// </summary>
        IndexOnly = 1,

        /// <summary>
        /// Only the working directory is examined for changes
        /// </summary>
        WorkDirOnly = 2
    }

    /// <summary>
    /// Options controlling the status behavior.
    /// </summary>
    public sealed class StatusOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusOptions"/> class.
        /// By default, both the index and the working directory will be scanned
        /// for status, and renames will be detected from changes staged in the
        /// index only.
        /// </summary>
        public StatusOptions()
        {
            DetectRenamesInIndex = true;
        }

        /// <summary>
        /// Which files should be scanned and returned
        /// </summary>
        public StatusShowOption Show { get; set; }

        /// <summary>
        /// Examine the staged changes for renames.
        /// </summary>
        public bool DetectRenamesInIndex { get; set; }

        /// <summary>
        /// Examine unstaged changes in the working directory for renames.
        /// </summary>
        public bool DetectRenamesInWorkDir { get; set; }

        /// <summary>
        /// Exclude submodules from being scanned for status
        /// </summary>
        public bool ExcludeSubmodules { get; set; }

        /// <summary>
        /// Recurse into ignored directories
        /// </summary>
        public bool RecurseIgnoredDirs { get; set; }

        /// <summary>
        /// Limit the scope of paths to consider to the provided pathspecs
        /// </summary>
        /// <remarks>
        /// If a PathSpec is given, the results from rename detection may
        /// not be accurate.
        /// </remarks>
        public string[] PathSpec { get; set; }

        /// <summary>
        /// When set to <c>true</c>, the PathSpec paths will be considered
        /// as explicit paths, and NOT as pathspecs containing globs.
        /// </summary>
        public bool DisablePathSpecMatch { get; set; }

        /// <summary>
        /// Include unaltered files when scanning for status
        /// </summary>
        /// <remarks>
        /// Unaltered meaning the file is identical in the working directory, the index and HEAD.
        /// </remarks>
        public bool IncludeUnaltered { get; set; }
    }
}
