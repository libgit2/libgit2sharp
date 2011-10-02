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
            int res = NativeMethods.git_repository_index(out handle, repo.Handle);
            Ensure.Success(res);
        }

        internal IndexSafeHandle Handle
        {
            get { return handle; }
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

                if (res == (int)GitErrorCode.GIT_ENOTFOUND)
                {
                    return null;
                }

                Ensure.Success(res, true);

                return this[(uint)res];
            }
        }

        private IndexEntry this[uint index]
        {
            get
            {
                IntPtr entryPtr = NativeMethods.git_index_get(handle, index);
                return IndexEntry.CreateFromPtr(repo, entryPtr);
            }
        }

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            handle.SafeDispose();
        }

        #endregion

        #region IEnumerable<IndexEntry> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator<IndexEntry> GetEnumerator()
        {
            for (uint i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public void Stage(string path)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            AddToIndex(path);

            UpdatePhysicalIndex();
        }

        public void Unstage(string path)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            string relativePath = BuildRelativePathFrom(repo, path);

            RemoveFromIndex(relativePath);

            RestorePotentialPreviousVersionOf(relativePath);

            UpdatePhysicalIndex();
        }

        public void Move(string sourcePath, string destinationPath)
        {
            Ensure.ArgumentNotNullOrEmptyString(sourcePath, "sourcepath");
            Ensure.ArgumentNotNullOrEmptyString(destinationPath, "destinationpath");

            string relativeSourcePath = BuildRelativePathFrom(repo, sourcePath);
            string relativeDestinationPath = BuildRelativePathFrom(repo, destinationPath);

            string wd = repo.Info.WorkingDirectory;
            if (Directory.Exists(Path.Combine(wd, relativeSourcePath)))
            {
                throw new NotImplementedException();
            }

            RemoveFromIndex(relativeSourcePath);

            File.Move(Path.Combine(wd, relativeSourcePath), Path.Combine(wd, relativeDestinationPath));

            AddToIndex(relativeDestinationPath);

            UpdatePhysicalIndex();
        }

        private void AddToIndex(string path)
        {
            int res = NativeMethods.git_index_add(handle, BuildRelativePathFrom(repo, path));
            Ensure.Success(res);
        }

        private void RemoveFromIndex(string path)
        {
            int res = NativeMethods.git_index_find(handle, BuildRelativePathFrom(repo, path));
            Ensure.Success(res, true);

            res = NativeMethods.git_index_remove(handle, res);
            Ensure.Success(res);
        }

        private void RestorePotentialPreviousVersionOf(string relativePath)
        {
            TreeEntry currentHeadBlob = repo.Head.Tip.Tree[relativePath];
            if ((currentHeadBlob == null) || currentHeadBlob.Type != GitObjectType.Blob)
            {
                return;
            }

            File.WriteAllBytes(Path.Combine(repo.Info.WorkingDirectory, relativePath), ((Blob)currentHeadBlob.Target).Content);
            AddToIndex(relativePath);
        }

        private void UpdatePhysicalIndex()
        {
            int res = NativeMethods.git_index_write(handle);
            Ensure.Success(res);
        }

        private static string BuildRelativePathFrom(Repository repo, string path) //TODO: To be removed when libgit2 natively implements this
        {
            if (!Path.IsPathRooted(path))
            {
                return path;
            }

            string normalizedPath = Path.GetFullPath(path);

            if (!normalizedPath.StartsWith(repo.Info.WorkingDirectory, StringComparison.Ordinal))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Unable to process file '{0}'. This file is not located under the working directory of the repository ('{1}').", normalizedPath, repo.Info.WorkingDirectory));
            }

            return normalizedPath.Substring(repo.Info.WorkingDirectory.Length);
        }

        public FileStatus RetrieveStatus(string filePath)
        {
            Ensure.ArgumentNotNullOrEmptyString(filePath, "filePath");

            string relativePath = BuildRelativePathFrom(repo, filePath);

            FileStatus status;

            int res = NativeMethods.git_status_file(out status, repo.Handle, relativePath);
            if (res == (int)GitErrorCode.GIT_ENOTFOUND)
            {
                return FileStatus.Nonexistent;
            }

            Ensure.Success(res);

            return status;
        }

        public RepositoryStatus RetrieveStatus()
        {
            return new RepositoryStatus(repo);
        }
    }
}
