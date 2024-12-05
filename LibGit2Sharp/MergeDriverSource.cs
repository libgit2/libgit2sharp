using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A merge driver source - describes the repository and file being merged.
    /// The MergeDriverSource is what is sent to the Merge Driver callback.
    /// </summary>
    public class MergeDriverSource
    {
        /// <summary>
        /// The repository containing the file to be merged
        /// </summary>
        public Repository Repository;
        /// <summary>
        /// The original content
        /// </summary>
        public IndexEntry Ancestor;
        /// <summary>
        /// The content to be merged (based on the Ancestor)
        /// </summary>
        public IndexEntry Ours;
        /// <summary>
        /// The already updated content (based on the Ancestor)
        /// </summary>
        public IndexEntry Theirs;

        /// <summary>
        /// Needed for mocking purposes
        /// </summary>
        protected MergeDriverSource() { }

        private MergeDriverSource(Repository repos, IndexEntry ancestor, IndexEntry ours, IndexEntry theirs)
        {
            Repository = repos;
            Ancestor = ancestor;
            Ours = ours;
            Theirs = theirs;
        }

        /// <summary>
        /// Take an unmanaged pointer and convert it to a merge driver source callback paramater
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        internal static unsafe MergeDriverSource FromNativePtr(IntPtr ptr)
        {
            return FromNativePtr((git_merge_driver_source*)ptr.ToPointer());
        }

        /// <summary>
        /// Take an unmanaged pointer and convert it to a merge driver source callback paramater
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        internal static unsafe MergeDriverSource FromNativePtr(git_merge_driver_source* ptr)
        {
            if (ptr == null)
                throw new ArgumentNullException(nameof(ptr), "The merge driver source must be defined");

            return new MergeDriverSource(
                new Repository(new RepositoryHandle(ptr->repository, false)),
                IndexEntry.BuildFromPtr(ptr->ancestor),
                IndexEntry.BuildFromPtr(ptr->ours),
                IndexEntry.BuildFromPtr(ptr->theirs));
        }
    }
}
