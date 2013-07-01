using System;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Option flags for `git_repository_open_ext`
    /// </summary>
    [Flags]
    internal enum RepositoryOpenFlags
    {
        /// <summary>
        /// Only open the repository if it can be
        ///  *   immediately found in the start_path.  Do not walk up from the
        ///  *   start_path looking at parent directories.
        /// </summary>
        NoSearch = (1 << 0), /* GIT_REPOSITORY_OPEN_NO_SEARCH */

        /// <summary>
        /// Unless this flag is set, open will not
        ///  *   continue searching across filesystem boundaries (i.e. when `st_dev`
        ///  *   changes from the `stat` system call).  (E.g. Searching in a user's home
        ///  *   directory "/home/user/source/" will not return "/.git/" as the found
        ///  *   repo if "/" is a different filesystem than "/home".)
        /// </summary>
        CrossFS = (1 << 1), /* GIT_REPOSITORY_OPEN_CROSS_FS */
    }
}
