using System;
using System.Collections.Generic;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A merge driver source - describes the direction of merging and the file being merged
    /// </summary>
    public class MergeDriverSource
    {
        private static Dictionary<long, Repository> reposesInFlight = new Dictionary<long, Repository>();
        private readonly IntPtr _reposPtr;
        private Repository _repos;

        /// <summary>
        /// Repository where merge is taking place
        /// </summary>
        /// <remarks>Marked virtual for xUnit</remarks>
        public virtual Repository Repository
        {
            get
            {
                if (_repos == null)
                    _repos = GetCachedRepos(_reposPtr);
                return _repos;
            }
        }

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
        /// Needed for mocking purposes
        /// </summary>
        protected MergeDriverSource() { }

        internal MergeDriverSource(IntPtr reposPtr, IndexEntry ancestor, IndexEntry ours, IndexEntry theirs)
        {
            _reposPtr = reposPtr;
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

            return new MergeDriverSource(new IntPtr(ptr->repository),
                IndexEntry.BuildFromPtr(ptr->ancestor),
                IndexEntry.BuildFromPtr(ptr->ours),
                IndexEntry.BuildFromPtr(ptr->theirs));
        }

        private static Repository GetCachedRepos(IntPtr reposPtr)
        {
            Repository repos;
            var cacheKey = reposPtr.ToInt64();
            lock (reposesInFlight)
            {
                if (!reposesInFlight.TryGetValue(cacheKey, out repos))
                {
                    var reposHandle = new RepositoryHandle(reposPtr, false);
                    repos = new Repository(reposHandle);
                    repos.RegisterForCleanup(reposHandle);
                    reposesInFlight.Add(cacheKey, repos);
                }
            }
            return repos;
        }

        internal static void OnMergeDone(IntPtr reposPtr)
        {
            if (reposPtr == null)
                return;

            lock (reposesInFlight)
            {
                Repository repos;
                var cacheKey = reposPtr.ToInt64();
                if (reposesInFlight.TryGetValue(cacheKey, out repos))
                {
                    reposesInFlight.Remove(cacheKey);
                    repos.SafeDispose();
                }
            }
        }
    }
}
