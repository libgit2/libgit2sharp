using System.Linq;
using System.IO;
using System.Collections.Generic;
using LibGit2Sharp;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public static partial class Commands
    {

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
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="path">The path of the file within the working directory.</param>
        public static void Remove(IRepository repository, string path)
        {
            Remove(repository, path, true, null);
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
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the file from the working directory, False otherwise.</param>
        public static void Remove(IRepository repository, string path, bool removeFromWorkingDirectory)
        {
            Remove(repository, path, removeFromWorkingDirectory, null);
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
        /// <param name="repository">The repository in which to operate</param>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the file from the working directory, False otherwise.</param>
        /// <param name="explicitPathsOptions">
        /// The passed <paramref name="path"/> will be treated as an explicit path.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public static void Remove(IRepository repository, string path, bool removeFromWorkingDirectory, ExplicitPathsOptions explicitPathsOptions)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(path, "path");

            Remove(repository, new[] { path }, removeFromWorkingDirectory, explicitPathsOptions);
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
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        public static void Remove(IRepository repository, IEnumerable<string> paths)
        {
            Remove(repository, paths, true, null);
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
        /// <param name="repository">The repository in which to operate</param>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the files from the working directory, False otherwise.</param>
        /// <param name="explicitPathsOptions">
        /// The passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public static void Remove(IRepository repository, IEnumerable<string> paths, bool removeFromWorkingDirectory, ExplicitPathsOptions explicitPathsOptions)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNullOrEmptyEnumerable<string>(paths, "paths");

            var pathsToDelete = paths.Where(p => Directory.Exists(Path.Combine(repository.Info.WorkingDirectory, p))).ToList();
            var notConflictedPaths = new List<string>();
            var index = repository.Index;

            foreach (var path in paths)
            {
                Ensure.ArgumentNotNullOrEmptyString(path, "path");

                var conflict = index.Conflicts[path];

                if (conflict != null)
                {
                    index.Remove(path);
                    pathsToDelete.Add(path);
                }
                else
                {
                    notConflictedPaths.Add(path);
                }
            }

            // Make sure status will see the changes from before this
            index.Write();

            if (notConflictedPaths.Count > 0)
            {
                pathsToDelete.AddRange(RemoveStagedItems(repository, notConflictedPaths, removeFromWorkingDirectory, explicitPathsOptions));
            }

            if (removeFromWorkingDirectory)
            {
                RemoveFilesAndFolders(repository, pathsToDelete);
            }

            index.Write();
        }

        private static void RemoveFilesAndFolders(IRepository repository, IEnumerable<string> pathsList)
        {
            string wd = repository.Info.WorkingDirectory;

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

        private static IEnumerable<string> RemoveStagedItems(IRepository repository, IEnumerable<string> paths, bool removeFromWorkingDirectory = true, ExplicitPathsOptions explicitPathsOptions = null)
        {
            var removed = new List<string>();
            using (var changes = repository.Diff.Compare<TreeChanges>(DiffModifiers.IncludeUnmodified | DiffModifiers.IncludeUntracked, paths, explicitPathsOptions))
            {
                var index = repository.Index;

                foreach (var treeEntryChanges in changes)
                {
                    var status = repository.RetrieveStatus(treeEntryChanges.Path);

                    switch (treeEntryChanges.Status)
                    {
                        case ChangeKind.Added:
                        case ChangeKind.Deleted:
                            removed.Add(treeEntryChanges.Path);
                            index.Remove(treeEntryChanges.Path);
                            break;

                        case ChangeKind.Unmodified:
                            if (removeFromWorkingDirectory && (
                                status.HasFlag(FileStatus.ModifiedInIndex) ||
                                status.HasFlag(FileStatus.NewInIndex)))
                            {
                                throw new RemoveFromIndexException("Unable to remove file '{0}', as it has changes staged in the index. You can call the Remove() method with removeFromWorkingDirectory=false if you want to remove it from the index only.",
                                    treeEntryChanges.Path);
                            }
                            removed.Add(treeEntryChanges.Path);
                            index.Remove(treeEntryChanges.Path);
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
                            removed.Add(treeEntryChanges.Path);
                            index.Remove(treeEntryChanges.Path);
                            continue;

                        default:
                            throw new RemoveFromIndexException("Unable to remove file '{0}'. Its current status is '{1}'.",
                                treeEntryChanges.Path,
                                treeEntryChanges.Status);
                    }
                }

                index.Write();

                return removed;
            }
        }
    }
}

