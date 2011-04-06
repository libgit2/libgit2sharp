using System;
using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A collection of commits in a <see cref = "Repository" />
    /// </summary>
    public class CommitCollection : IEnumerable<Commit>
    {
        private readonly Repository repo;
        private CommitEnumerator enumerator;
        private string pushedSha;
        private GitSortOptions sortOptions = GitSortOptions.None;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "CommitCollection" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        public CommitCollection(Repository repo)
        {
            this.repo = repo;
        }

        private CommitEnumerator Enumerator
        {
            get { return enumerator ?? (enumerator = new CommitEnumerator(repo)); }
        }

        /// <summary>
        ///   Gets the <see cref = "LibGit2Sharp.Commit" /> with the specified sha. (This is identical to calling Lookup/<Commit />(sha) on the repo)
        /// </summary>
        public Commit this[string sha]
        {
            get { return repo.Lookup<Commit>(sha); }
        }

        #region IEnumerable<Commit> Members

        public IEnumerator<Commit> GetEnumerator()
        {
            Enumerator.Sort(sortOptions);
            if (string.IsNullOrEmpty(pushedSha))
            {
                throw new NotImplementedException();
            }

            Enumerator.Push(pushedSha);
            return Enumerator;
        }

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
            sortOptions = options;
            return this;
        }

        /// <summary>
        ///   Starts enumeratoring the <see cref = "CommitCollection" /> at the specified branch.
        /// </summary>
        /// <param name = "branch">The branch.</param>
        /// <returns></returns>
        public CommitCollection StartingAt(Branch branch)
        {
            Ensure.ArgumentNotNull(branch, "branch");

            pushedSha = branch.Tip.Sha;
            return this;
        }

        /// <summary>
        ///   Starts enumeratoring the <see cref = "CommitCollection" /> at the specified reference.
        /// </summary>
        /// <param name = "reference">The reference.</param>
        /// <returns></returns>
        public CommitCollection StartingAt(Reference reference)
        {
            Ensure.ArgumentNotNull(reference, "reference");

            var directReference = reference.ResolveToDirectReference();
            pushedSha = directReference.Target.Sha;
            return this;
        }

        /// <summary>
        ///   Starts enumeratoring the <see cref = "CommitCollection" /> at the specified sha.
        /// </summary>
        /// <param name = "sha">The sha.</param>
        /// <returns></returns>
        public CommitCollection StartingAt(string sha)
        {
            Ensure.ArgumentNotNullOrEmptyString(sha, "sha");

            pushedSha = sha;
            return this;
        }

        #region Nested type: CommitEnumerator

        public class CommitEnumerator : IEnumerator<Commit>
        {
            private readonly Repository repo;
            private readonly IntPtr walker = IntPtr.Zero;
            private bool disposed;

            public CommitEnumerator(Repository repo)
            {
                this.repo = repo;
                int res = NativeMethods.git_revwalk_new(out walker, repo.Handle);
                Ensure.Success(res);
            }

            #region IEnumerator<Commit> Members

            public Commit Current { get; private set; }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                GitOid oid;
                var res = NativeMethods.git_revwalk_next(out oid, walker);
                if (res == (int) GitErrorCode.GIT_EREVWALKOVER) return false;

                Current = repo.Lookup<Commit>(new ObjectId(oid));

                return true;
            }

            public void Reset()
            {
                NativeMethods.git_revwalk_reset(walker);
            }

            #endregion

            private void Dispose(bool disposing)
            {
                // Check to see if Dispose has already been called.
                if (!disposed)
                {
                    // If disposing equals true, dispose all managed
                    // and unmanaged resources.
                    if (disposing)
                    {
                        // Dispose managed resources.
                    }

                    // Call the appropriate methods to clean up
                    // unmanaged resources here.
                    NativeMethods.git_revwalk_free(walker);

                    // Note disposing has been done.
                    disposed = true;
                }
            }

            ~CommitEnumerator()
            {
                // Do not re-create Dispose clean-up code here.
                // Calling Dispose(false) is optimal in terms of
                // readability and maintainability.
                Dispose(false);
            }

            public void Push(string sha)
            {
                var id = new ObjectId(sha);
                var oid = id.Oid;
                int res = NativeMethods.git_revwalk_push(walker, ref oid);
                Ensure.Success(res);
            }

            public void Sort(GitSortOptions options)
            {
                NativeMethods.git_revwalk_sorting(walker, options);
            }
        }

        #endregion
    }
}