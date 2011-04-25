using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Index : IDisposable
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

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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