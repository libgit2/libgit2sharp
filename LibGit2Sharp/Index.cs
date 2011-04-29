using System;
using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Index : IEnumerable<IndexEntry>, IDisposable
    {
        private readonly IndexSafeHandle handle;
        private readonly Repository repo;

        public Index(Repository repo)
        {
            this.repo = repo;
            var res = NativeMethods.git_index_open_inrepo(out handle, repo.Handle);
            Ensure.Success(res);
        }

        public int Count
        {
            get { return (int) NativeMethods.git_index_entrycount(handle); }
        }

        public IndexEntry this[string path]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(path, "path");

                return this[NativeMethods.git_index_find(handle, path)];
            }
        }

        private IndexEntry this[int index]
        {
            get
            {
                var entryPtr = NativeMethods.git_index_get(handle, index);
                return IndexEntry.CreateFromPtr(entryPtr);
            }
        }

        public IEnumerable<IndexEntry> Modified
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<IndexEntry> Staged
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerable<IndexEntry> Untracked
        {
            get { throw new NotImplementedException(); }
        }

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (handle != null && !handle.IsInvalid)
            {
                handle.Dispose();
            }
        }

        #endregion

        #region IEnumerable<IndexEntry> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public IEnumerator<IndexEntry> GetEnumerator()
        {
            var list = new List<IndexEntry>();
            for (int i = 0; i < Count; i++)
            {
                list.Add(this[i]); //TODO: yield return?
            }
            return list.GetEnumerator();
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



        public void Stage(string path)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            var res = NativeMethods.git_index_add(handle, path);
            Ensure.Success(res);
        }

        public void Unstage(string path)
        {
            throw new NotImplementedException();
        }
    }
}