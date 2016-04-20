namespace LibGit2Sharp
{
    /// <summary>
    /// Class to convey information about the repository that is being operated on
    /// for operations that can recurse into submodules.
    /// </summary>
    public class RepositoryOperationContext
    {
        /// <summary>
        /// Needed for mocking.
        /// </summary>
        protected RepositoryOperationContext()
        { }

        /// <summary>
        /// Constructor suitable for use on the repository the main
        /// operation is being run on (i.e. the super project, not a submodule).
        /// </summary>
        /// <param name="repositoryPath">The path of the repository being operated on.</param>
        /// <param name="remoteUrl">The URL that this operation will download from.</param>
        internal RepositoryOperationContext(string repositoryPath, string remoteUrl)
            : this(repositoryPath, remoteUrl, string.Empty, string.Empty, 0)
        { }

        /// <summary>
        /// Constructor suitable for use on the sub repositories.
        /// </summary>
        /// <param name="repositoryPath">The path of the repository being operated on.</param>
        /// <param name="remoteUrl">The URL that this operation will download from.</param>
        /// <param name="parentRepositoryPath">The path to the super repository.</param>
        /// <param name="submoduleName">The logical name of this submodule.</param>
        /// <param name="recursionDepth">The depth of this sub repository from the original super repository.</param>
        internal RepositoryOperationContext(
            string repositoryPath,
            string remoteUrl,
            string parentRepositoryPath,
            string submoduleName, int recursionDepth)
        {
            RepositoryPath = repositoryPath;
            RemoteUrl = remoteUrl;
            ParentRepositoryPath = parentRepositoryPath;
            SubmoduleName = submoduleName;
            RecursionDepth = recursionDepth;
        }

        /// <summary>
        /// If this is a submodule repository, the full path to the parent
        /// repository. If this is not a submodule repository, then
        /// this is empty.
        /// </summary>
        public virtual string ParentRepositoryPath { get; private set; }

        /// <summary>
        /// The recursion depth for the current repository being operated on
        /// with respect to the repository the original operation was run
        /// against. The initial repository has a recursion depth of 0,
        /// the 1st level of subrepositories has a recursion depth of 1.
        /// </summary>
        public virtual int RecursionDepth { get; private set; }

        /// <summary>
        /// The remote URL this operation will work against, if any.
        /// </summary>
        public virtual string RemoteUrl { get; private set; }

        /// <summary>
        /// Full path of the repository.
        /// </summary>
        public virtual string RepositoryPath { get; private set; }

        /// <summary>
        /// The submodule's logical name in the parent repository, if this is a
        /// submodule repository. If this is not a submodule repository, then
        /// this is empty.
        /// </summary>
        public virtual string SubmoduleName { get; private set; }
    }
}
