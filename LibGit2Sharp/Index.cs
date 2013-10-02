using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// The Index is a staging area between the Working directory and the Repository.
    /// It's used to prepare and aggregate the changes that will be part of the next commit.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Index : IEnumerable<IndexEntry>
    {
        private readonly IndexSafeHandle handle;
        private readonly Repository repo;
        private readonly ConflictCollection conflicts;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Index()
        { }

        internal Index(Repository repo)
        {
            this.repo = repo;

            handle = Proxy.git_repository_index(repo.Handle);
            conflicts = new ConflictCollection(repo);

            repo.RegisterForCleanup(handle);
        }

        internal Index(Repository repo, string indexPath)
        {
            this.repo = repo;

            handle = Proxy.git_index_open(indexPath);
            Proxy.git_repository_set_index(repo.Handle, handle);
            conflicts = new ConflictCollection(repo);

            repo.RegisterForCleanup(handle);
        }

        internal IndexSafeHandle Handle
        {
            get { return handle; }
        }

        /// <summary>
        /// Gets the number of <see cref="IndexEntry"/> in the index.
        /// </summary>
        public virtual int Count
        {
            get { return Proxy.git_index_entrycount(handle); }
        }

        /// <summary>
        /// Determines if the index is free from conflicts.
        /// </summary>
        public virtual bool IsFullyMerged
        {
            get { return !Proxy.git_index_has_conflicts(handle); }
        }

        /// <summary>
        /// Gets the <see cref="IndexEntry"/> with the specified relative path.
        /// </summary>
        public virtual IndexEntry this[string path]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(path, "path");

                IndexEntrySafeHandle entryHandle = Proxy.git_index_get_bypath(handle, path, 0);
                return IndexEntry.BuildFromPtr(entryHandle);
            }
        }

        private IndexEntry this[int index]
        {
            get
            {
                IndexEntrySafeHandle entryHandle = Proxy.git_index_get_byindex(handle, (UIntPtr)index);
                return IndexEntry.BuildFromPtr(entryHandle);
            }
        }

        #region IEnumerable<IndexEntry> Members

        private List<IndexEntry> AllIndexEntries()
        {
            var entryCount = Count;
            var list = new List<IndexEntry>(entryCount);

            for (int i = 0; i < entryCount; i++)
            {
                list.Add(this[i]);
            }

            return list;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<IndexEntry> GetEnumerator()
        {
            return AllIndexEntries().GetEnumerator();
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
        /// Promotes to the staging area the latest modifications of a file in the working directory (addition, updation or removal).
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="path"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public virtual void Stage(string path, ExplicitPathsOptions explicitPathsOptions = null)
        {
            Ensure.ArgumentNotNull(path, "path");

            Stage(new[] { path }, explicitPathsOptions);
        }

        /// <summary>
        /// Promotes to the staging area the latest modifications of a collection of files in the working directory (addition, updation or removal).
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public virtual void Stage(IEnumerable<string> paths, ExplicitPathsOptions explicitPathsOptions = null)
        {
            Ensure.ArgumentNotNull(paths, "paths");

            var changes = repo.Diff.Compare<TreeChanges>(DiffModifiers.IncludeUntracked | DiffModifiers.IncludeIgnored, paths, explicitPathsOptions);

            foreach (var treeEntryChanges in changes)
            {
                switch (treeEntryChanges.Status)
                {
                    case ChangeKind.Unmodified:
                        continue;

                    case ChangeKind.Deleted:
                        RemoveFromIndex(treeEntryChanges.Path);
                        continue;

                    case ChangeKind.Added:
                        /* Fall through */
                    case ChangeKind.Modified:
                        AddToIndex(treeEntryChanges.Path);
                        continue;

                    default:
                        throw new InvalidOperationException(
                            string.Format(CultureInfo.InvariantCulture, "Entry '{0}' bears an unexpected ChangeKind '{1}'", treeEntryChanges.Path, treeEntryChanges.Status));
                }
            }

            UpdatePhysicalIndex();
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="path"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public virtual void Unstage(string path, ExplicitPathsOptions explicitPathsOptions = null)
        {
            Ensure.ArgumentNotNull(path, "path");

            Unstage(new[] { path }, explicitPathsOptions);
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a collection of file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public virtual void Unstage(IEnumerable<string> paths, ExplicitPathsOptions explicitPathsOptions = null)
        {
            Ensure.ArgumentNotNull(paths, "paths");

            if (repo.Info.IsHeadUnborn)
            {
                var changes = repo.Diff.Compare<TreeChanges>(null, DiffTargets.Index, paths, explicitPathsOptions);

                Reset(changes);
            }
            else
            {
                repo.Reset("HEAD", paths, explicitPathsOptions);
            }
        }

        /// <summary>
        /// Moves and/or renames a file in the working directory and promotes the change to the staging area.
        /// </summary>
        /// <param name="sourcePath">The path of the file within the working directory which has to be moved/renamed.</param>
        /// <param name="destinationPath">The target path of the file within the working directory.</param>
        public virtual void Move(string sourcePath, string destinationPath)
        {
            Move(new[] { sourcePath }, new[] { destinationPath });
        }

        /// <summary>
        /// Moves and/or renames a collection of files in the working directory and promotes the changes to the staging area.
        /// </summary>
        /// <param name="sourcePaths">The paths of the files within the working directory which have to be moved/renamed.</param>
        /// <param name="destinationPaths">The target paths of the files within the working directory.</param>
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
                if (sourceStatus.HasAny(new Enum[] { FileStatus.Nonexistent, FileStatus.Removed, FileStatus.Untracked, FileStatus.Missing }))
                {
                    throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture, "Unable to move file '{0}'. Its current status is '{1}'.", sourcePath, sourceStatus));
                }

                FileStatus desStatus = keyValuePair.Value.Item2;
                if (desStatus.HasAny(new Enum[] { FileStatus.Nonexistent, FileStatus.Missing }))
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
        /// Removes a file from the staging area, and optionally removes it from the working directory as well.
        /// <para>
        ///   If the file has already been deleted from the working directory, this method will only deal
        ///   with promoting the removal to the staging area.
        /// </para>
        /// <para>
        ///   The default behavior is to remove the file from the working directory as well.
        /// </para>
        /// <para>
        ///   When not passing a <paramref name="explicitPathsOptions"/>, the passed path will be treated as
        ///   a pathspec. You can for example use it to pass the relative path to a folder inside the working directory,
        ///   so that all files beneath this folders, and the folder itself, will be removed.
        /// </para>
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the file from the working directory, False otherwise.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="path"/> will be treated as an explicit path.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public virtual void Remove(string path, bool removeFromWorkingDirectory = true, ExplicitPathsOptions explicitPathsOptions = null)
        {
            Ensure.ArgumentNotNull(path, "path");

            Remove(new[] { path }, removeFromWorkingDirectory, explicitPathsOptions);
        }

        /// <summary>
        /// Removes a collection of fileS from the staging, and optionally removes them from the working directory as well.
        /// <para>
        ///   If a file has already been deleted from the working directory, this method will only deal
        ///   with promoting the removal to the staging area.
        /// </para>
        /// <para>
        ///   The default behavior is to remove the files from the working directory as well.
        /// </para>
        /// <para>
        ///   When not passing a <paramref name="explicitPathsOptions"/>, the passed paths will be treated as
        ///   a pathspec. You can for example use it to pass the relative paths to folders inside the working directory,
        ///   so that all files beneath these folders, and the folders themselves, will be removed.
        /// </para>
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the files from the working directory, False otherwise.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public virtual void Remove(IEnumerable<string> paths, bool removeFromWorkingDirectory = true, ExplicitPathsOptions explicitPathsOptions = null)
        {
            var pathsList = paths.ToList();
            var changes = repo.Diff.Compare<TreeChanges>(DiffModifiers.IncludeUnmodified | DiffModifiers.IncludeUntracked, pathsList, explicitPathsOptions);

            var pathsTodelete = pathsList.Where(p => Directory.Exists(Path.Combine(repo.Info.WorkingDirectory, p))).ToList();

            foreach (var treeEntryChanges in changes)
            {
                var status = repo.Index.RetrieveStatus(treeEntryChanges.Path);

                switch (treeEntryChanges.Status)
                {
                    case ChangeKind.Added:
                    case ChangeKind.Deleted:
                        pathsTodelete.Add(RemoveFromIndex(treeEntryChanges.Path));
                        break;

                    case ChangeKind.Unmodified:
                        if (removeFromWorkingDirectory && (
                            status.HasFlag(FileStatus.Staged) ||
                            status.HasFlag(FileStatus.Added)))
                        {
                            throw new RemoveFromIndexException(string.Format(CultureInfo.InvariantCulture, "Unable to remove file '{0}', as it has changes staged in the index. You can call the Remove() method with removeFromWorkingDirectory=false if you want to remove it from the index only.",
                                treeEntryChanges.Path));
                        }
                        pathsTodelete.Add(RemoveFromIndex(treeEntryChanges.Path));
                        continue;

                    case ChangeKind.Modified:
                        if (status.HasFlag(FileStatus.Modified) && status.HasFlag(FileStatus.Staged))
                        {
                            throw new RemoveFromIndexException(string.Format(CultureInfo.InvariantCulture, "Unable to remove file '{0}', as it has staged content different from both the working directory and the HEAD.",
                                treeEntryChanges.Path));
                        }
                        if (removeFromWorkingDirectory)
                        {
                            throw new RemoveFromIndexException(string.Format(CultureInfo.InvariantCulture, "Unable to remove file '{0}', as it has local modifications. You can call the Remove() method with removeFromWorkingDirectory=false if you want to remove it from the index only.",
                                treeEntryChanges.Path));
                        }
                        pathsTodelete.Add(RemoveFromIndex(treeEntryChanges.Path));
                        continue;


                    default:
                        throw new RemoveFromIndexException(string.Format(CultureInfo.InvariantCulture, "Unable to remove file '{0}'. Its current status is '{1}'.",
                            treeEntryChanges.Path, treeEntryChanges.Status));
                }
            }

            if (removeFromWorkingDirectory)
            {
                RemoveFilesAndFolders(pathsTodelete);
            }

            UpdatePhysicalIndex();
        }

        private void RemoveFilesAndFolders(IEnumerable<string> pathsList)
        {
            string wd = repo.Info.WorkingDirectory;

            foreach (string path in pathsList)
            {
                string fileName = Path.Combine(wd, path);

                if (Directory.Exists(fileName))
                {
                    Directory.Delete(fileName, true);
                    continue;
                }

                if (!File.Exists(fileName))
                {
                    continue;
                }

                File.Delete(fileName);
            }
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
            string relativePath = repo.BuildRelativePathFrom(path);
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
            if (!repo.Submodules.TryStage(relativePath, true))
            {
                Proxy.git_index_add_bypath(handle, relativePath);
            }
        }

        private string RemoveFromIndex(string relativePath)
        {
            Proxy.git_index_remove_bypath(handle, relativePath);

            return relativePath;
        }

        private void UpdatePhysicalIndex()
        {
            Proxy.git_index_write(handle);
        }

        /// <summary>
        /// Retrieves the state of a file in the working directory, comparing it against the staging area and the latest commmit.
        /// </summary>
        /// <param name="filePath">The relative path within the working directory to the file.</param>
        /// <returns>A <see cref="FileStatus"/> representing the state of the <paramref name="filePath"/> parameter.</returns>
        public virtual FileStatus RetrieveStatus(string filePath)
        {
            Ensure.ArgumentNotNullOrEmptyString(filePath, "filePath");

            string relativePath = repo.BuildRelativePathFrom(filePath);

            return Proxy.git_status_file(repo.Handle, relativePath);
        }

        /// <summary>
        /// Retrieves the state of all files in the working directory, comparing them against the staging area and the latest commmit.
        /// </summary>
        /// <param name="options">If set, the options that control the status investigation.</param>
        /// <returns>A <see cref="RepositoryStatus"/> holding the state of all the files.</returns>
        public virtual RepositoryStatus RetrieveStatus(StatusOptions options = null)
        {
            ReloadFromDisk();

            return new RepositoryStatus(repo, options);
        }

        internal void Reset(TreeChanges changes)
        {
            foreach (TreeEntryChanges treeEntryChanges in changes)
            {
                switch (treeEntryChanges.Status)
                {
                    case ChangeKind.Unmodified:
                        continue;

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

        /// <summary>
        ///  Gets the conflicts that exist.
        /// </summary>
        public virtual ConflictCollection Conflicts
        {
            get
            {
                return conflicts;
            }
        }

        private void ReplaceIndexEntryWith(TreeEntryChanges treeEntryChanges)
        {
            var indexEntry = new GitIndexEntry
            {
                Mode = (uint)treeEntryChanges.OldMode,
                oid = treeEntryChanges.OldOid.Oid,
                Path = StrictFilePathMarshaler.FromManaged(treeEntryChanges.OldPath),
            };

            Proxy.git_index_add(handle, indexEntry);
            EncodingMarshaler.Cleanup(indexEntry.Path);
        }

        internal void ReloadFromDisk()
        {
            Proxy.git_index_read(handle);
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
