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

        public int Count
        {
            get { return (int) NativeMethods.git_index_entrycount(handle); }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IEnumerable<IndexEntry> Members

        public IEnumerator<IndexEntry> GetEnumerator()
        {
            var list = new List<IndexEntry>();
            for (int i = 0; i < Count; i++)
            {
                list.Add(this[i]);
            }
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (handle != null && !handle.IsInvalid)
            {
                handle.Dispose();
            }
        }
    }
}