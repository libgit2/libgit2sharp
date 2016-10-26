using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A merge driver source - describes the direction of merging and the file being merged
    /// </summary>
    public class MergeDriverSource : IDisposable
    {
        /// <summary>
        /// Repository where merge is taking place
        /// </summary>
        public readonly Repository Repository;

        /// <summary>
        /// Ancestor of merge
        /// </summary>
        public readonly IndexEntry Ancestor;

        /// <summary>
        /// Own changes to merge
        /// </summary>
        public readonly IndexEntry Ours;

        /// <summary>
        /// Other changes to merge
        /// </summary>
        public readonly IndexEntry Theirs;

        /// <summary>
        /// Detect redundant Dispose() calls
        /// </summary>
        private bool disposedValue = false;

        /// <summary>
        /// Needed for mocking purposes
        /// </summary>
        protected MergeDriverSource() { }

        internal MergeDriverSource(Repository repos, IndexEntry ancestor, IndexEntry ours, IndexEntry theirs)
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
                throw new ArgumentException();

            var reposHandle = new RepositoryHandle(ptr->repository, false);
            var repos = new Repository(reposHandle);
            repos.RegisterForCleanup(reposHandle);

            return new MergeDriverSource(repos,
                IndexEntry.BuildFromPtr(ptr->ancestor),
                IndexEntry.BuildFromPtr(ptr->ours),
                IndexEntry.BuildFromPtr(ptr->theirs));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Repository.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
