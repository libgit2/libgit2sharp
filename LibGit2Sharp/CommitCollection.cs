using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A collection of commits in a <see cref = "Repository" />
    /// </summary>
    public class CommitCollection : IEnumerable<Commit>
    {
        private readonly Repository repo;
        private string pushedSha;
        private GitSortOptions sortOptions = GitSortOptions.Time;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "CommitCollection" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        internal CommitCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the <see cref = "LibGit2Sharp.Commit" /> with the specified sha. (This is identical to calling Lookup/<Commit />(sha) on the repo)
        /// </summary>
        public Commit this[string sha]
        {
            get { return repo.Lookup<Commit>(sha); }
        }

        /// <summary>
        ///   Gets the Count of commits (This is a fast count that does not hydrate real commit objects)
        /// </summary>
        public int Count
        {
            get
            {
                var count = 0;
                using (var enumerator = new CommitEnumerator(repo))
                {
                    enumerator.Sort(sortOptions);
                    enumerator.Push(pushedSha);
                    while (enumerator.MoveNext()) count++;
                }
                return count;
            }
        }

        #region IEnumerable<Commit> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Commit> GetEnumerator()
        {
            if (string.IsNullOrEmpty(pushedSha))
            {
                throw new NotImplementedException();
            }

            var enumerator = new CommitEnumerator(repo);
            enumerator.Sort(sortOptions);
            enumerator.Push(pushedSha);
            return enumerator;
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
        ///   Sorts <see cref = "CommitCollection" /> with the specified options.
        /// </summary>
        /// <param name = "options">The options.</param>
        /// <returns></returns>
        public CommitCollection SortBy(GitSortOptions options)
        {
            return new CommitCollection(repo) { sortOptions = options, pushedSha = pushedSha };
        }

        /// <summary>
        ///   Starts enumeratoring the <see cref = "CommitCollection" /> at the specified branch.
        /// </summary>
        /// <param name = "branch">The branch.</param>
        /// <returns></returns>
        public CommitCollection StartingAt(Branch branch)
        {
            Ensure.ArgumentNotNull(branch, "branch");

            return new CommitCollection(repo) { sortOptions = sortOptions, pushedSha = branch.Tip.Sha };
        }

        /// <summary>
        ///   Starts enumeratoring the <see cref = "CommitCollection" /> at the specified reference.
        /// </summary>
        /// <param name = "reference">The reference.</param>
        /// <returns></returns>
        public CommitCollection StartingAt(Reference reference)
        {
            Ensure.ArgumentNotNull(reference, "reference");

            return new CommitCollection(repo) { sortOptions = sortOptions, pushedSha = reference.ResolveToDirectReference().Target.Sha };
        }

        /// <summary>
        ///   Starts enumeratoring the <see cref = "CommitCollection" /> at the specified sha.
        /// </summary>
        /// <param name = "sha">The sha.</param>
        /// <returns></returns>
        public CommitCollection StartingAt(string sha)
        {
            Ensure.ArgumentNotNullOrEmptyString(sha, "sha");

            return new CommitCollection(repo) { sortOptions = sortOptions, pushedSha = sha };
        }

        #region Nested type: CommitEnumerator

        private class CommitEnumerator : IEnumerator<Commit>
        {
            private readonly Repository repo;
            private readonly RevWalkerSafeHandle handle;
            private ObjectId currentOid;

            public CommitEnumerator(Repository repo)
            {
                this.repo = repo;
                int res = NativeMethods.git_revwalk_new(out handle, repo.Handle);
                Ensure.Success(res);
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

            public void Push(string sha)
            {
                var id = new ObjectId(sha);
                var oid = id.Oid;
                int res = NativeMethods.git_revwalk_push(handle, ref oid);
                Ensure.Success(res);
            }

            public void Sort(GitSortOptions options)
            {
                NativeMethods.git_revwalk_sorting(handle, options);
            }
        }

        #endregion
    }
}