using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A collection of commits in a <see cref = "Repository" />
    /// </summary>
    public class CommitCollection : IEnumerable<Commit>
    {
        private readonly Repository repo;
        private ObjectId pushedObjectId;
        private readonly GitSortOptions sortOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref = "CommitCollection"/> class.
        /// The commits will be enumerated according in reverse chronological order.
        /// </summary>
        /// <param name = "repo">The repository.</param>
        internal CommitCollection(Repository repo) : this (repo, GitSortOptions.Time)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "CommitCollection" /> class.
        /// </summary>
        /// <param name = "repo">The repository.</param>
        /// <param name = "sortingStrategy">The sorting strategy which should be applied when enumerating the commits.</param>
        internal CommitCollection(Repository repo, GitSortOptions sortingStrategy)
        {
            this.repo = repo;
            sortOptions = sortingStrategy;
        }

        /// <summary>
        ///   Gets the <see cref = "LibGit2Sharp.Commit" /> with the specified sha. (This is identical to calling Lookup/<Commit />(sha) on the repo)
        /// </summary>
        public Commit this[string sha]
        {
            get { return repo.Lookup<Commit>(sha); }
        }

        /// <summary>
        /// Gets the current sorting strategy applied when enumerating the collection
        /// </summary>
        public GitSortOptions SortedBy
        {
            get { return sortOptions; }
        }

        #region IEnumerable<Commit> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Commit> GetEnumerator()
        {
            if (pushedObjectId == null)
            {
                throw new NotImplementedException();
            }

            return new CommitEnumerator(repo, pushedObjectId, sortOptions);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///   Sorts <see cref = "CommitCollection" /> according to the specified strategy.
        /// </summary>
        /// <param name = "sortingStrategy">The sorting strategy to be applied when enumerating the commits.</param>
        /// <returns></returns>
        public CommitCollection SortBy(GitSortOptions sortingStrategy)
        {
            return new CommitCollection(repo, sortingStrategy) { pushedObjectId = pushedObjectId };
        }

        /// <summary>
        ///   Starts enumeratoring the <see cref = "CommitCollection" /> at the specified sha.
        /// </summary>
        ///  <param name = "shaOrReferenceName">The sha or reference canonical name to use.</param>
        /// <returns></returns>
        public CommitCollection StartingAt(string shaOrReferenceName)
        {
            Ensure.ArgumentNotNullOrEmptyString(shaOrReferenceName, "shaOrReferenceName");

            GitObject gitObj = repo.Lookup(shaOrReferenceName);

            if (gitObj == null) // TODO: Should we check the type? Git-log allows TagAnnotation oid as parameter. But what about Blobs and Trees?
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "No valid git object pointed at by '{0}' exists in the repository.", shaOrReferenceName));
            }

            return new CommitCollection(repo, sortOptions) { pushedObjectId = gitObj.Id };
        }

        #region Nested type: CommitEnumerator

        private class CommitEnumerator : IEnumerator<Commit>
        {
            private readonly Repository repo;
            private readonly RevWalkerSafeHandle handle;
            private ObjectId currentOid;

            public CommitEnumerator(Repository repo, ObjectId pushedOid, GitSortOptions sortingStrategy)
            {
                this.repo = repo;
                int res = NativeMethods.git_revwalk_new(out handle, repo.Handle);
                Ensure.Success(res);

                Sort(sortingStrategy);
                Push(pushedOid);
            }

            #region IEnumerator<Commit> Members

            public Commit Current
            {
                get
                {
                    if (currentOid == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return repo.Lookup<Commit>(currentOid);
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                GitOid oid;
                var res = NativeMethods.git_revwalk_next(out oid, handle);
                
                if (res == (int)GitErrorCode.GIT_EREVWALKOVER)
                {
                    return false;
                }

                Ensure.Success(res);

                currentOid = new ObjectId(oid);

                return true;
            }

            public void Reset()
            {
                NativeMethods.git_revwalk_reset(handle);
            }

            #endregion

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            
            private void Dispose(bool disposing)
            {
                if (handle == null || handle.IsInvalid)
                {
                    return;
                }

                handle.Dispose();
            }

            private void Push(ObjectId pushedOid)
            {
                var oid = pushedOid.Oid;
                int res = NativeMethods.git_revwalk_push(handle, ref oid);
                Ensure.Success(res);
            }

            private void Sort(GitSortOptions options)
            {
                NativeMethods.git_revwalk_sorting(handle, options);
            }
        }

        #endregion
    }
}