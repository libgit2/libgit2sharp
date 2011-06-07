using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Index : IEnumerable<IndexEntry>, IDisposable
    {
        private readonly IndexSafeHandle handle;
        private readonly Repository repo;

        internal Index(Repository repo)
        {
            this.repo = repo;
            var res = NativeMethods.git_repository_index(out handle, repo.Handle);
            Ensure.Success(res);
        }

        public int Count
        {
            get { return (int)NativeMethods.git_index_entrycount(handle); }
        }

        public IndexEntry this[string path]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(path, "path");

                int res = NativeMethods.git_index_find(handle, path);
                Ensure.Success(res, true);

                return this[res];
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
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
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

            var res = NativeMethods.git_index_add(handle, BuildRelativePathFrom(path));
            Ensure.Success(res);

            UpdatePhysicalIndex();
        }

        public void Unstage(string path)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            var res = NativeMethods.git_index_find(handle, BuildRelativePathFrom(path));
            Ensure.Success(res, true);

            res = NativeMethods.git_index_remove(handle, res);
            Ensure.Success(res);

            UpdatePhysicalIndex();
        }

        private void UpdatePhysicalIndex()
        {
            int res = NativeMethods.git_index_write(handle);
            Ensure.Success(res);
        }

        private string BuildRelativePathFrom(string path)   //TODO: To be removed when libgit2 natively implements this 
        {
            if (!Path.IsPathRooted(path))
            {
                return path;
            }

            var normalizedPath = Path.GetFullPath(path);

            if (!normalizedPath.StartsWith(repo.Info.WorkingDirectory, StringComparison.Ordinal))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Unable to stage file '{0}'. This file is not located under the working directory of the repository ('{1}').", normalizedPath, repo.Info.WorkingDirectory));
            }

            return normalizedPath.Substring(repo.Info.WorkingDirectory.Length);
        }
    }
}