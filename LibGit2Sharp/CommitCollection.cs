#region  Copyright (c) 2011 LibGit2Sharp committers

//  The MIT License
//  
//  Copyright (c) 2011 LibGit2Sharp committers
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.

#endregion

using System;
using System.Collections;
using System.Collections.Generic;

namespace LibGit2Sharp
{
    public class CommitCollection : IEnumerable<Commit>
    {
        private readonly Repository repo;
        private CommitEnumerator enumerator;
        private string pushedSha;
        private GitSortOptions sortOptions = GitSortOptions.None;

        public CommitCollection(Repository repo)
        {
            this.repo = repo;
        }

        private CommitEnumerator Enumerator
        {
            get { return enumerator ?? (enumerator = new CommitEnumerator(repo)); }
        }

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

        public CommitCollection SortBy(GitSortOptions options)
        {
            sortOptions = options;
            return this;
        }

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
                int res = NativeMethods.git_revwalk_new(out walker, repo.RepoPtr);
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
                if (res == (int) GitErrorCodes.GIT_EREVWALKOVER) return false;

                Current = repo.Lookup<Commit>(oid);

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
                var oid = GitOid.FromSha(sha);
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