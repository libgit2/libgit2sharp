using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The Index is a staging area between the Working directory and the Repository.
    ///   It's used to prepare and aggregate the changes that will be part of the next commit.
    /// </summary>
    public class Index : IEnumerable<IndexEntry>
    {
        private readonly IndexSafeHandle handle;
        private readonly Repository repo;

        private static readonly Utf8Marshaler utf8Marshaler = new Utf8Marshaler();

        internal Index(Repository repo)
        {
            this.repo = repo;
            int res = NativeMethods.git_repository_index(out handle, repo.Handle);
            Ensure.Success(res);

            repo.RegisterForCleanup(handle);
        }

        internal IndexSafeHandle Handle
        {
            get { return handle; }
        }

        /// <summary>
        ///   Gets the number of <see cref = "IndexEntry" /> in the index.
        /// </summary>
        public int Count
        {
            get { return (int)NativeMethods.git_index_entrycount(handle); }
        }

        /// <summary>
        ///   Gets the <see cref = "IndexEntry" /> with the specified relative path.
        /// </summary>
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

        /// <summary>
        ///   Promotes to the staging area the latest modifications of a file in the working directory (addition, updation or removal).
        /// </summary>
        /// <param name = "path">The path of the file within the working directory.</param>
        public void Stage(string path)
        {
            Ensure.ArgumentNotNull(path, "path");

            Stage(new[] { path });
        }

        /// <summary>
        ///   Promotes to the staging area the latest modifications of a collection of files in the working directory (addition, updation or removal).
        /// </summary>
        /// <param name = "paths">The collection of paths of the files within the working directory.</param>
        public void Stage(IEnumerable<string> paths)
        {
            //TODO: Stage() should support following use cases:
            // - Recursively staging the content of a directory

            IDictionary<string, FileStatus> batch = PrepareBatch(paths);

            foreach (KeyValuePair<string, FileStatus> kvp in batch)
            {
                if (Directory.Exists(kvp.Key))
                {
                    throw new NotImplementedException();
                }

                if (!kvp.Value.Has(FileStatus.Nonexistent))
                {
                    continue;
                }

                throw new LibGit2Exception(string.Format(CultureInfo.InvariantCulture, "Can not stage '{0}'. The file does not exist.", kvp.Key));
            }

            foreach (KeyValuePair<string, FileStatus> kvp in batch)
            {
                string relativePath = kvp.Key;
                FileStatus fileStatus = kvp.Value;

                if (fileStatus.Has(FileStatus.Missing))
                {
                    RemoveFromIndex(relativePath);
                }
                else
                {
                    AddToIndex(relativePath);
                }
            }

            UpdatePhysicalIndex();
        }

        /// <summary>
        ///   Removes from the staging area all the modifications of a file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name = "path">The path of the file within the working directory.</param>
        public void Unstage(string path)
        {
            Ensure.ArgumentNotNull(path, "path");

            Unstage(new[] { path });
        }

        /// <summary>
        ///   Removes from the staging area all the modifications of a collection of file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name = "paths">The collection of paths of the files within the working directory.</param>
        public void Unstage(IEnumerable<string> paths)
        {
            IDictionary<string, FileStatus> batch = PrepareBatch(paths);

            foreach (KeyValuePair<string, FileStatus> kvp in batch)
            {
                if (Directory.Exists(kvp.Key))
                {
                    throw new NotImplementedException();
                }
            }

            foreach (KeyValuePair<string, FileStatus> kvp in batch)
            {
                bool doesExistInIndex =
                    !(kvp.Value.Has(FileStatus.Nonexistent) || kvp.Value.Has(FileStatus.Removed) ||
                      kvp.Value.Has(FileStatus.Untracked));

                if (doesExistInIndex)
                {
                    RemoveFromIndex(kvp.Key);
                }

                RestorePotentialPreviousVersionOfHeadIntoIndex(kvp.Key);
            }

            UpdatePhysicalIndex();
        }

        /// <summary>
        ///   Moves and/or renames a file in the working directory and promotes the change to the staging area.
        /// </summary>
        /// <param name = "sourcePath">The path of the file within the working directory which has to be moved/renamed.</param>
        /// <param name = "destinationPath">The target path of the file within the working directory.</param>
        public void Move(string sourcePath, string destinationPath)
        {
            Move(new[] { sourcePath }, new[] { destinationPath });
        }

        /// <summary>
        ///   Moves and/or renames a collection of files in the working directory and promotes the changes to the staging area.
        /// </summary>
        /// <param name = "sourcePaths">The paths of the files within the working directory which have to be moved/renamed.</param>
        /// <param name = "destinationPaths">The target paths of the files within the working directory.</param>
        public void Move(IEnumerable<string> sourcePaths, IEnumerable<string> destinationPaths)
        {
            Ensure.ArgumentNotNull(sourcePaths, "sourcePaths");
            Ensure.ArgumentNotNull(destinationPaths, "destinationPaths");

            //TODO: Move() should support following use cases:
            // - Moving a file under a directory ('file' and 'dir' -> 'dir/file')
            // - Moving a directory (and its content) under another directory ('dir1' and 'dir2' -> 'dir2/dir1/*')

            //TODO: Move() should throw when:
            // - Moving a directory under a file

            IDictionary<Tuple<string, FileStatus>, Tuple<string, FileStatus>> batch = PrepareBatch(sourcePaths, destinationPaths);

            if (batch.Count == 0)
            {
                throw new ArgumentNullException("sourcePaths");
            }

            foreach (KeyValuePair<Tuple<string, FileStatus>, Tuple<string, FileStatus>> keyValuePair in batch)
            {
                string sourcePath = keyValuePair.Key.Item1;
                string destPath = keyValuePair.Value.Item1;

                if (Directory.Exists(sourcePath) || Directory.Exists(destPath))
                {
                    throw new NotImplementedException();
                }

                FileStatus sourceStatus = keyValuePair.Key.Item2;
                if (sourceStatus.HasAny(new[] { FileStatus.Nonexistent, FileStatus.Removed, FileStatus.Untracked, FileStatus.Missing }))
                {
                    throw new LibGit2Exception(string.Format(CultureInfo.InvariantCulture, "Unable to move file '{0}'. Its current status is '{1}'.", sourcePath, Enum.GetName(typeof(FileStatus), sourceStatus)));
                }

                FileStatus desStatus = keyValuePair.Value.Item2;
                if (desStatus.HasAny(new[] { FileStatus.Nonexistent, FileStatus.Missing }))
                {
                    continue;
                }

                throw new LibGit2Exception(string.Format(CultureInfo.InvariantCulture, "Unable to overwrite file '{0}'. Its current status is '{1}'.", destPath, Enum.GetName(typeof(FileStatus), desStatus)));
            }

            string wd = repo.Info.WorkingDirectory;
            foreach (KeyValuePair<Tuple<string, FileStatus>, Tuple<string, FileStatus>> keyValuePair in batch)
            {
                string from = keyValuePair.Key.Item1;
                string to = keyValuePair.Value.Item1;

                RemoveFromIndex(from);
                File.Move(Path.Combine(wd, from), Path.Combine(wd, to));
                AddToIndex(to);
            }

            UpdatePhysicalIndex();
        }

        /// <summary>
        ///   Removes a file from the working directory and promotes the removal to the staging area.
        ///   <para>
        ///     If the file has already been deleted from the working directory, this method will only deal
        ///     with promoting the removal to the staging area.
        ///   </para>
        /// </summary>
        /// <param name = "path">The path of the file within the working directory.</param>
        public void Remove(string path)
        {
            Ensure.ArgumentNotNull(path, "path");

            Remove(new[] { path });
        }

        /// <summary>
        ///   Removes a collection of files from the working directory and promotes the removal to the staging area.
        ///   <para>
        ///     If a file has already been deleted from the working directory, this method will only deal
        ///     with promoting the removal to the staging area.
        ///   </para>
        /// </summary>
        /// <param name = "paths">The collection of paths of the files within the working directory.</param>
        public void Remove(IEnumerable<string> paths)
        {
            //TODO: Remove() should support following use cases:
            // - Removing a directory and its content

            IDictionary<string, FileStatus> batch = PrepareBatch(paths);

            foreach (KeyValuePair<string, FileStatus> keyValuePair in batch)
            {
                if (Directory.Exists(keyValuePair.Key))
                {
                    throw new NotImplementedException();
                }

                if (!keyValuePair.Value.HasAny(new[] { FileStatus.Nonexistent, FileStatus.Removed, FileStatus.Modified, FileStatus.Untracked }))
                {
                    continue;
                }

                throw new LibGit2Exception(string.Format(CultureInfo.InvariantCulture, "Unable to remove file '{0}'. Its current status is '{1}'.", keyValuePair.Key, Enum.GetName(typeof(FileStatus), keyValuePair.Value)));
            }

            string wd = repo.Info.WorkingDirectory;
            foreach (KeyValuePair<string, FileStatus> keyValuePair in batch)
            {
                RemoveFromIndex(keyValuePair.Key);

                if (File.Exists(Path.Combine(wd, keyValuePair.Key)))
                {
                    File.Delete(Path.Combine(wd, keyValuePair.Key));
                }
            }

            UpdatePhysicalIndex();
        }

        private IDictionary<string, FileStatus> PrepareBatch(IEnumerable<string> paths)
        {
            Ensure.ArgumentNotNull(paths, "paths");

            IDictionary<string, FileStatus> dic = new Dictionary<string, FileStatus>();

            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new ArgumentException("At least one provided path is either null or empty.", "paths");
                }

                string relativePath = BuildRelativePathFrom(repo, path);
                FileStatus fileStatus = RetrieveStatus(relativePath);

                dic.Add(relativePath, fileStatus);
            }

            if (dic.Count == 0)
            {
                throw new ArgumentException("No path has been provided.", "paths");
            }

            return dic;
        }

        private IDictionary<Tuple<string, FileStatus>, Tuple<string, FileStatus>> PrepareBatch(IEnumerable<string> leftPaths, IEnumerable<string> rightPaths)
        {
            IDictionary<Tuple<string, FileStatus>, Tuple<string, FileStatus>> dic = new Dictionary<Tuple<string, FileStatus>, Tuple<string, FileStatus>>();

            IEnumerator<string> leftEnum = leftPaths.GetEnumerator();
            IEnumerator<string> rightEnum = rightPaths.GetEnumerator();

            while (Enumerate(leftEnum, rightEnum))
            {
                Tuple<string, FileStatus> from = BuildFrom(leftEnum.Current);
                Tuple<string, FileStatus> to = BuildFrom(rightEnum.Current);
                dic.Add(from, to);
            }

            return dic;
        }

        private Tuple<string, FileStatus> BuildFrom(string path)
        {
            string relativePath = BuildRelativePathFrom(repo, path);
            return new Tuple<string, FileStatus>(relativePath, RetrieveStatus(relativePath));
        }

        private bool Enumerate(IEnumerator<string> leftEnum, IEnumerator<string> rightEnum)
        {
            bool isLeftEoF = leftEnum.MoveNext();
            bool isRightEoF = rightEnum.MoveNext();

            if (isLeftEoF == isRightEoF)
            {
                return isLeftEoF;
            }

            throw new ArgumentException("The collection of paths are of different lengths.");
        }

        private void AddToIndex(string relativePath)
        {
            int res = NativeMethods.git_index_add(handle, relativePath);
            Ensure.Success(res);
        }

        private void RemoveFromIndex(string relativePath)
        {
            int res = NativeMethods.git_index_find(handle, relativePath);
            Ensure.Success(res, true);

            res = NativeMethods.git_index_remove(handle, res);
            Ensure.Success(res);
        }

        private void RestorePotentialPreviousVersionOfHeadIntoIndex(string relativePath)
        {
            TreeEntry treeEntry = repo.Head[relativePath];
            if (treeEntry == null || treeEntry.Type != GitObjectType.Blob)
            {
                return;
            }

            var indexEntry = new GitIndexEntry
                                 {
                                     Mode = (uint)treeEntry.Attributes,
                                     oid = treeEntry.Target.Id.Oid,
                                     Path = utf8Marshaler.MarshalManagedToNative(relativePath),
                                 };

            Ensure.Success(NativeMethods.git_index_add2(handle, indexEntry));
            utf8Marshaler.CleanUpNativeData(indexEntry.Path);
        }

        private void UpdatePhysicalIndex()
        {
            int res = NativeMethods.git_index_write(handle);
            Ensure.Success(res);
        }

        private static string BuildRelativePathFrom(Repository repo, string path)
        {
            //TODO: To be removed when libgit2 natively implements this
            if (!Path.IsPathRooted(path))
            {
                return path;
            }

            string normalizedPath = Path.GetFullPath(path);

            if (!normalizedPath.StartsWith(repo.Info.WorkingDirectory, StringComparison.Ordinal))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                                                          "Unable to process file '{0}'. This file is not located under the working directory of the repository ('{1}').",
                                                          normalizedPath, repo.Info.WorkingDirectory));
            }

            return normalizedPath.Substring(repo.Info.WorkingDirectory.Length);
        }

        /// <summary>
        ///   Retrieves the state of a file in the working directory, comparing it against the staging area and the latest commmit.
        /// </summary>
        /// <param name = "filePath">The relative path within the working directory to the file.</param>
        /// <returns>A <see cref = "FileStatus" /> representing the state of the <paramref name = "filePath" /> parameter.</returns>
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

        /// <summary>
        ///   Retrieves the state of all files in the working directory, comparing them against the staging area and the latest commmit.
        /// </summary>
        /// <returns>A <see cref = "RepositoryStatus" /> holding the state of all the files.</returns>
        public RepositoryStatus RetrieveStatus()
        {
            return new RepositoryStatus(repo);
        }

        internal void ReplaceContentWithTree(Tree tree)
        {
            using (var nativeTree = new ObjectSafeWrapper(tree.Id, repo))
            {
                int res = NativeMethods.git_index_read_tree(Handle, nativeTree.ObjectPtr);
                Ensure.Success(res);
                UpdatePhysicalIndex();
            }
        }
    }
}
