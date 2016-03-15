using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    public partial class Repository
    {
        /// <summary>
        /// Promotes to the staging area the latest modifications of a file in the working directory (addition, updation or removal).
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        public void Stage(string path)
        {
            Stage(path, null);
        }

        /// <summary>
        /// Promotes to the staging area the latest modifications of a collection of files in the working directory (addition, updation or removal).
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        public void Stage(IEnumerable<string> paths)
        {
            Stage(paths, null);
        }

        /// <summary>
        /// Promotes to the staging area the latest modifications of a file in the working directory (addition, updation or removal).
        ///
        /// If this path is ignored by configuration then it will not be staged unless <see cref="StageOptions.IncludeIgnored"/> is unset.
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="stageOptions">Determines how paths will be staged.</param>
        public void Stage(string path, StageOptions stageOptions)
        {
            Ensure.ArgumentNotNull(path, "path");

            Stage(new[] { path }, stageOptions);
        }

        /// <summary>
        /// Promotes to the staging area the latest modifications of a collection of files in the working directory (addition, updation or removal).
        ///
        /// Any paths (even those listed explicitly) that are ignored by configuration will not be staged unless <see cref="StageOptions.IncludeIgnored"/> is unset.
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="stageOptions">Determines how paths will be staged.</param>
        public void Stage(IEnumerable<string> paths, StageOptions stageOptions)
        {
            Ensure.ArgumentNotNull(paths, "paths");

            DiffModifiers diffModifiers = DiffModifiers.IncludeUntracked;
            ExplicitPathsOptions explicitPathsOptions = stageOptions != null ? stageOptions.ExplicitPathsOptions : null;

            if (stageOptions != null && stageOptions.IncludeIgnored)
            {
                diffModifiers |= DiffModifiers.IncludeIgnored;
            }

            var changes = Diff.Compare<TreeChanges>(diffModifiers, paths, explicitPathsOptions,
                new CompareOptions { Similarity = SimilarityOptions.None });

            var unexpectedTypesOfChanges = changes
                .Where(
                    tec => tec.Status != ChangeKind.Added &&
                    tec.Status != ChangeKind.Modified &&
                    tec.Status != ChangeKind.Conflicted &&
                    tec.Status != ChangeKind.Unmodified &&
                    tec.Status != ChangeKind.Deleted).ToList();

            if (unexpectedTypesOfChanges.Count > 0)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture,
                        "Entry '{0}' bears an unexpected ChangeKind '{1}'",
                        unexpectedTypesOfChanges[0].Path, unexpectedTypesOfChanges[0].Status));
            }

            /* Remove files from the index that don't exist on disk */
            foreach (TreeEntryChanges treeEntryChanges in changes)
            {
                switch (treeEntryChanges.Status)
                {
                    case ChangeKind.Conflicted:
                        if (!treeEntryChanges.Exists)
                        {
                            RemoveFromIndex(treeEntryChanges.Path);
                        }
                        break;

                    case ChangeKind.Deleted:
                        RemoveFromIndex(treeEntryChanges.Path);
                        break;

                    default:
                        continue;
                }
            }

            foreach (TreeEntryChanges treeEntryChanges in changes)
            {
                switch (treeEntryChanges.Status)
                {
                    case ChangeKind.Added:
                    case ChangeKind.Modified:
                        AddToIndex(treeEntryChanges.Path);
                        break;

                    case ChangeKind.Conflicted:
                        if (treeEntryChanges.Exists)
                        {
                            AddToIndex(treeEntryChanges.Path);
                        }
                        break;

                    default:
                        continue;
                }
            }

            UpdatePhysicalIndex();
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        public void Unstage(string path)
        {
            Unstage(path, null);
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a collection of file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        public void Unstage(IEnumerable<string> paths)
        {
            Unstage(paths, null);
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="explicitPathsOptions">
        /// The passed <paramref name="path"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public void Unstage(string path, ExplicitPathsOptions explicitPathsOptions)
        {
            Ensure.ArgumentNotNull(path, "path");

            Unstage(new[] { path }, explicitPathsOptions);
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a collection of file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="explicitPathsOptions">
        /// The passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public void Unstage(IEnumerable<string> paths, ExplicitPathsOptions explicitPathsOptions)
        {
            Ensure.ArgumentNotNull(paths, "paths");

            if (Info.IsHeadUnborn)
            {
                var changes = Diff.Compare<TreeChanges>(null, DiffTargets.Index, paths, explicitPathsOptions, new CompareOptions { Similarity = SimilarityOptions.None });

                Index.Replace(changes);
            }
            else
            {
                Index.Replace(Head.Tip, paths, explicitPathsOptions);
            }
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
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        public void Remove(string path)
        {
            Remove(path, true, null);
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
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the file from the working directory, False otherwise.</param>
        public void Remove(string path, bool removeFromWorkingDirectory)
        {
            Remove(path, removeFromWorkingDirectory, null);
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
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        public void Remove(IEnumerable<string> paths)
        {
            Remove(paths, true, null);
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
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the files from the working directory, False otherwise.</param>
        public void Remove(IEnumerable<string> paths, bool removeFromWorkingDirectory)
        {
            Remove(paths, removeFromWorkingDirectory, null);
        }

        /// <summary>
        /// Retrieves the state of a file in the working directory, comparing it against the staging area and the latest commit.
        /// </summary>
        /// <param name="filePath">The relative path within the working directory to the file.</param>
        /// <returns>A <see cref="FileStatus"/> representing the state of the <paramref name="filePath"/> parameter.</returns>
        public FileStatus RetrieveStatus(string filePath)
        {
            Ensure.ArgumentNotNullOrEmptyString(filePath, "filePath");

            string relativePath = this.BuildRelativePathFrom(filePath);

            return Proxy.git_status_file(Handle, relativePath);
        }

        /// <summary>
        /// Retrieves the state of all files in the working directory, comparing them against the staging area and the latest commit.
        /// </summary>
        /// <returns>A <see cref="RepositoryStatus"/> holding the state of all the files.</returns>
        public RepositoryStatus RetrieveStatus()
        {
            Proxy.git_index_read(Index.Handle);
            return new RepositoryStatus(this, null);
        }

        /// <summary>
        /// Retrieves the state of all files in the working directory, comparing them against the staging area and the latest commit.
        /// </summary>
        /// <param name="options">If set, the options that control the status investigation.</param>
        /// <returns>A <see cref="RepositoryStatus"/> holding the state of all the files.</returns>
        public RepositoryStatus RetrieveStatus(StatusOptions options)
        {
            ReloadFromDisk();

            return new RepositoryStatus(this, options);
        }


        /// <summary>
        /// Moves and/or renames a file in the working directory and promotes the change to the staging area.
        /// </summary>
        /// <param name="sourcePath">The path of the file within the working directory which has to be moved/renamed.</param>
        /// <param name="destinationPath">The target path of the file within the working directory.</param>
        public void Move(string sourcePath, string destinationPath)
        {
            Move(new[] { sourcePath }, new[] { destinationPath });
        }

        /// <summary>
        /// Moves and/or renames a collection of files in the working directory and promotes the changes to the staging area.
        /// </summary>
        /// <param name="sourcePaths">The paths of the files within the working directory which have to be moved/renamed.</param>
        /// <param name="destinationPaths">The target paths of the files within the working directory.</param>
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
                if (sourceStatus.HasAny(new Enum[] { FileStatus.Nonexistent, FileStatus.DeletedFromIndex, FileStatus.NewInWorkdir, FileStatus.DeletedFromWorkdir }))
                {
                    throw new LibGit2SharpException("Unable to move file '{0}'. Its current status is '{1}'.",
                        sourcePath,
                        sourceStatus);
                }

                FileStatus desStatus = keyValuePair.Value.Item2;
                if (desStatus.HasAny(new Enum[] { FileStatus.Nonexistent, FileStatus.DeletedFromWorkdir }))
                {
                    continue;
                }

                throw new LibGit2SharpException("Unable to overwrite file '{0}'. Its current status is '{1}'.",
                    destPath,
                    desStatus);
            }

            string wd = Info.WorkingDirectory;
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
        /// The passed <paramref name="path"/> will be treated as an explicit path.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public void Remove(string path, bool removeFromWorkingDirectory, ExplicitPathsOptions explicitPathsOptions)
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
        /// The passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public void Remove(IEnumerable<string> paths, bool removeFromWorkingDirectory, ExplicitPathsOptions explicitPathsOptions)
        {
            Ensure.ArgumentNotNullOrEmptyEnumerable<string>(paths, "paths");

            var pathsToDelete = paths.Where(p => Directory.Exists(Path.Combine(Info.WorkingDirectory, p))).ToList();
            var notConflictedPaths = new List<string>();

            foreach (var path in paths)
            {
                Ensure.ArgumentNotNullOrEmptyString(path, "path");

                var conflict = Index.Conflicts[path];

                if (conflict != null)
                {
                    pathsToDelete.Add(RemoveFromIndex(path));
                }
                else
                {
                    notConflictedPaths.Add(path);
                }
            }

            if (notConflictedPaths.Count > 0)
            {
                pathsToDelete.AddRange(RemoveStagedItems(notConflictedPaths, removeFromWorkingDirectory, explicitPathsOptions));
            }

            if (removeFromWorkingDirectory)
            {
                RemoveFilesAndFolders(pathsToDelete);
            }

            UpdatePhysicalIndex();
        }

        internal void ReloadFromDisk()
        {
            Proxy.git_index_read(Index.Handle);
        }

        private void AddToIndex(string relativePath)
        {
            if (!Submodules.TryStage(relativePath, true))
            {
                Proxy.git_index_add_bypath(Index.Handle, relativePath);
            }
        }

        private string RemoveFromIndex(string relativePath)
        {
            Proxy.git_index_remove_bypath(Index.Handle, relativePath);

            return relativePath;
        }

        private void UpdatePhysicalIndex()
        {
            Proxy.git_index_write(Index.Handle);
        }

        private Tuple<string, FileStatus> BuildFrom(string path)
        {
            string relativePath = this.BuildRelativePathFrom(path);
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

        private void RemoveFilesAndFolders(IEnumerable<string> pathsList)
        {
            string wd = Info.WorkingDirectory;

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

        private IEnumerable<string> RemoveStagedItems(IEnumerable<string> paths, bool removeFromWorkingDirectory = true, ExplicitPathsOptions explicitPathsOptions = null)
        {
            var removed = new List<string>();
            var changes = Diff.Compare<TreeChanges>(DiffModifiers.IncludeUnmodified | DiffModifiers.IncludeUntracked, paths, explicitPathsOptions);

            foreach (var treeEntryChanges in changes)
            {
                var status = RetrieveStatus(treeEntryChanges.Path);

                switch (treeEntryChanges.Status)
                {
                    case ChangeKind.Added:
                    case ChangeKind.Deleted:
                        removed.Add(RemoveFromIndex(treeEntryChanges.Path));
                        break;

                    case ChangeKind.Unmodified:
                        if (removeFromWorkingDirectory && (
                            status.HasFlag(FileStatus.ModifiedInIndex) ||
                            status.HasFlag(FileStatus.NewInIndex)))
                        {
                            throw new RemoveFromIndexException("Unable to remove file '{0}', as it has changes staged in the index. You can call the Remove() method with removeFromWorkingDirectory=false if you want to remove it from the index only.",
                                treeEntryChanges.Path);
                        }
                        removed.Add(RemoveFromIndex(treeEntryChanges.Path));
                        continue;

                    case ChangeKind.Modified:
                        if (status.HasFlag(FileStatus.ModifiedInWorkdir) && status.HasFlag(FileStatus.ModifiedInIndex))
                        {
                            throw new RemoveFromIndexException("Unable to remove file '{0}', as it has staged content different from both the working directory and the HEAD.",
                                treeEntryChanges.Path);
                        }
                        if (removeFromWorkingDirectory)
                        {
                            throw new RemoveFromIndexException("Unable to remove file '{0}', as it has local modifications. You can call the Remove() method with removeFromWorkingDirectory=false if you want to remove it from the index only.",
                                treeEntryChanges.Path);
                        }
                        removed.Add(RemoveFromIndex(treeEntryChanges.Path));
                        continue;

                    default:
                        throw new RemoveFromIndexException("Unable to remove file '{0}'. Its current status is '{1}'.",
                            treeEntryChanges.Path,
                            treeEntryChanges.Status);
                }
            }

            return removed;
        }

        /// <summary>
        /// Clean the working tree by removing files that are not under version control.
        /// </summary>
        public void RemoveUntrackedFiles()
        {
            var options = new GitCheckoutOpts
            {
                version = 1,
                checkout_strategy = CheckoutStrategy.GIT_CHECKOUT_REMOVE_UNTRACKED
                    | CheckoutStrategy.GIT_CHECKOUT_ALLOW_CONFLICTS,
            };

            Proxy.git_checkout_index(Handle, new NullGitObjectSafeHandle(), ref options);
        }
    }
}

