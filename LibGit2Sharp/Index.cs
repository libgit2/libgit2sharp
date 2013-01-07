using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The Index is a staging area between the Working directory and the Repository.
    ///   It's used to prepare and aggregate the changes that will be part of the next commit.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Index : IEnumerable<IndexEntry>
    {
        private readonly IndexSafeHandle handle;
        private readonly Repository repo;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Index()
        { }

        internal Index(Repository repo)
        {
            this.repo = repo;

            handle = Proxy.git_repository_index(repo.Handle);
            repo.RegisterForCleanup(handle);
        }

        internal Index(Repository repo, string indexPath)
        {
            this.repo = repo;

            handle = Proxy.git_index_open(indexPath);
            Proxy.git_repository_set_index(repo.Handle, handle);

            repo.RegisterForCleanup(handle);
        }

        internal IndexSafeHandle Handle
        {
            get { return handle; }
        }

        /// <summary>
        ///   Gets the number of <see cref = "IndexEntry" /> in the index.
        /// </summary>
        public virtual int Count
        {
            get { return Proxy.git_index_entrycount(handle); }
        }

        /// <summary>
        ///   Determines if the index is free from conflicts.
        /// </summary>
        public virtual bool IsFullyMerged
        {
            get { return !Proxy.git_index_has_conflicts(handle); }
        }
        
        /// <summary>
        ///   Gets the <see cref = "IndexEntry" /> with the specified relative path.
        /// </summary>
        public virtual IndexEntry this[string path]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(path, "path");

                IndexEntrySafeHandle entryHandle = Proxy.git_index_get_bypath(handle, path, 0);
                return IndexEntry.BuildFromPtr(repo, entryHandle);
            }
        }

        private IndexEntry this[int index]
        {
            get
            {
                IndexEntrySafeHandle entryHandle = Proxy.git_index_get_byindex(handle, (UIntPtr)index);
                return IndexEntry.BuildFromPtr(repo, entryHandle);
            }
        }

        #region IEnumerable<IndexEntry> Members

        private class OrdinalComparer<T> : IComparer<T>
        {
            Func<T, string> accessor;

            public OrdinalComparer(Func<T, string> accessor)
            {
                this.accessor = accessor;
            }

            public int Compare(T x, T y)
            {
                return string.CompareOrdinal(accessor(x), accessor(y));
            }
        }

        private List<IndexEntry> AllIndexEntries()
        {
            var list = new List<IndexEntry>();

            for (int i = 0; i < Count; i++)
            {
                list.Add(this[i]);
            }

            list.Sort(new OrdinalComparer<IndexEntry>(i => i.Path));
            return list;
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<IndexEntry> GetEnumerator()
        {
            return AllIndexEntries().GetEnumerator();
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
        public virtual void Stage(string path)
        {
            Ensure.ArgumentNotNull(path, "path");

            Stage(new[] { path });
        }

        /// <summary>
        ///   Promotes to the staging area the latest modifications of a collection of files in the working directory (addition, updation or removal).
        /// </summary>
        /// <param name = "paths">The collection of paths of the files within the working directory.</param>
        public virtual void Stage(IEnumerable<string> paths)
        {
            //TODO: Stage() should support following use cases:
            // - Recursively staging the content of a directory

            IEnumerable<KeyValuePair<string, FileStatus>> batch = PrepareBatch(paths);

            foreach (KeyValuePair<string, FileStatus> kvp in batch)
            {
                if (Directory.Exists(kvp.Key))
                {
                    throw new NotImplementedException();
                }

                if (!kvp.Value.HasFlag(FileStatus.Nonexistent))
                {
                    continue;
                }

                throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture, "Can not stage '{0}'. The file does not exist.", kvp.Key));
            }

            foreach (KeyValuePair<string, FileStatus> kvp in batch)
            {
                string relativePath = kvp.Key;
                FileStatus fileStatus = kvp.Value;

                if (fileStatus.HasFlag(FileStatus.Missing))
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
        public virtual void Unstage(string path)
        {
            Ensure.ArgumentNotNull(path, "path");

            Unstage(new[] { path });
        }

        /// <summary>
        ///   Removes from the staging area all the modifications of a collection of file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name = "paths">The collection of paths of the files within the working directory.</param>
        public virtual void Unstage(IEnumerable<string> paths)
        {
            Ensure.ArgumentNotNull(paths, "paths");

            if (repo.Info.IsHeadOrphaned)
            {
                TreeChanges changes = repo.Diff.Compare(null, DiffTargets.Index, paths);

                Reset(changes);
            }
            else
            {
                repo.Reset("HEAD", paths);
            }
        }

        /// <summary>
        ///   Moves and/or renames a file in the working directory and promotes the change to the staging area.
        /// </summary>
        /// <param name = "sourcePath">The path of the file within the working directory which has to be moved/renamed.</param>
        /// <param name = "destinationPath">The target path of the file within the working directory.</param>
        public virtual void Move(string sourcePath, string destinationPath)
        {
            Move(new[] { sourcePath }, new[] { destinationPath });
        }

        /// <summary>
        ///   Moves and/or renames a collection of files in the working directory and promotes the changes to the staging area.
        /// </summary>
        /// <param name = "sourcePaths">The paths of the files within the working directory which have to be moved/renamed.</param>
        /// <param name = "destinationPaths">The target paths of the files within the working directory.</param>
        public virtual void Move(IEnumerable<string> sourcePaths, IEnumerable<string> destinationPaths)
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
                    throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture, "Unable to move file '{0}'. Its current status is '{1}'.", sourcePath, sourceStatus));
                }

                FileStatus desStatus = keyValuePair.Value.Item2;
                if (desStatus.HasAny(new[] { FileStatus.Nonexistent, FileStatus.Missing }))
                {
                    continue;
                }

                throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture, "Unable to overwrite file '{0}'. Its current status is '{1}'.", destPath, desStatus));
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
        public virtual void Remove(string path)
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
        public virtual void Remove(IEnumerable<string> paths)
        {
            //TODO: Remove() should support following use cases:
            // - Removing a directory and its content

            IEnumerable<KeyValuePair<string, FileStatus>> batch = PrepareBatch(paths);

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

                throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture, "Unable to remove file '{0}'. Its current status is '{1}'.", keyValuePair.Key, keyValuePair.Value));
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

        private IEnumerable<KeyValuePair<string, FileStatus>> PrepareBatch(IEnumerable<string> paths)
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

        private static bool Enumerate(IEnumerator<string> leftEnum, IEnumerator<string> rightEnum)
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
            Proxy.git_index_add_from_workdir(handle, relativePath);
        }

        private void RemoveFromIndex(string relativePath)
        {
            Proxy.git_index_remove(handle, relativePath, 0);
        }

        private void UpdatePhysicalIndex()
        {
            Proxy.git_index_write(handle);
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
        public virtual FileStatus RetrieveStatus(string filePath)
        {
            Ensure.ArgumentNotNullOrEmptyString(filePath, "filePath");

            string relativePath = BuildRelativePathFrom(repo, filePath);

            return Proxy.git_status_file(repo.Handle, relativePath);
        }

        /// <summary>
        ///   Retrieves the state of all files in the working directory, comparing them against the staging area and the latest commmit.
        /// </summary>
        /// <returns>A <see cref = "RepositoryStatus" /> holding the state of all the files.</returns>
        public virtual RepositoryStatus RetrieveStatus()
        {
            return new RepositoryStatus(repo);
        }

        internal void Reset(TreeChanges changes)
        {
            foreach (TreeEntryChanges treeEntryChanges in changes)
            {
                switch (treeEntryChanges.Status)
                {
                    case ChangeKind.Added:
                        RemoveFromIndex(treeEntryChanges.Path);
                        continue;

                    case ChangeKind.Deleted:
                        /* Fall through */
                    case ChangeKind.Modified:
                        ReplaceIndexEntryWith(treeEntryChanges);    
                        continue;

                    default:
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Entry '{0}' bears an unexpected ChangeKind '{1}'", treeEntryChanges.Path, treeEntryChanges.Status));
                }
            }

            UpdatePhysicalIndex();
        }

        private void ReplaceIndexEntryWith(TreeEntryChanges treeEntryChanges)
        {
            var indexEntry = new GitIndexEntry
            {
                Mode = (uint)treeEntryChanges.OldMode,
                oid = treeEntryChanges.OldOid.Oid,
                Path = FilePathMarshaler.FromManaged(treeEntryChanges.OldPath),
            };

            Proxy.git_index_add(handle, indexEntry);
            Marshal.FreeHGlobal(indexEntry.Path);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "Count = {0}", Count);
            }
        }
    }
}
