using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using LibGit2Sharp;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public static partial class Commands
    {
        /// <summary>
        /// Promotes to the staging area the latest modifications of a file in the working directory (addition, updation or removal).
        ///
        /// If this path is ignored by configuration then it will not be staged unless <see cref="StageOptions.IncludeIgnored"/> is unset.
        /// </summary>
        /// <param name="repository">The repository in which to act</param>
        /// <param name="path">The path of the file within the working directory.</param>
        public static void Stage(IRepository repository, string path)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(path, "path");

            Stage(repository, new[] { path }, null);
        }

        /// <summary>
        /// Promotes to the staging area the latest modifications of a file in the working directory (addition, updation or removal).
        ///
        /// If this path is ignored by configuration then it will not be staged unless <see cref="StageOptions.IncludeIgnored"/> is unset.
        /// </summary>
        /// <param name="repository">The repository in which to act</param>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="stageOptions">Determines how paths will be staged.</param>
        public static void Stage(IRepository repository, string path, StageOptions stageOptions)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(path, "path");

            Stage(repository, new[] { path }, stageOptions);
        }

        /// <summary>
        /// Promotes to the staging area the latest modifications of a collection of files in the working directory (addition, updation or removal).
        ///
        /// Any paths (even those listed explicitly) that are ignored by configuration will not be staged unless <see cref="StageOptions.IncludeIgnored"/> is unset.
        /// </summary>
        /// <param name="repository">The repository in which to act</param>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        public static void Stage(IRepository repository, IEnumerable<string> paths)
        {
            Stage(repository, paths, null);
        }

        /// <summary>
        /// Promotes to the staging area the latest modifications of a collection of files in the working directory (addition, updation or removal).
        ///
        /// Any paths (even those listed explicitly) that are ignored by configuration will not be staged unless <see cref="StageOptions.IncludeIgnored"/> is unset.
        /// </summary>
        /// <param name="repository">The repository in which to act</param>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="stageOptions">Determines how paths will be staged.</param>
        public static void Stage(IRepository repository, IEnumerable<string> paths, StageOptions stageOptions)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(paths, "paths");

            DiffModifiers diffModifiers = DiffModifiers.IncludeUntracked;
            ExplicitPathsOptions explicitPathsOptions = stageOptions != null ? stageOptions.ExplicitPathsOptions : null;

            if (stageOptions != null && stageOptions.IncludeIgnored)
            {
                diffModifiers |= DiffModifiers.IncludeIgnored;
            }

            using (var changes = repository.Diff.Compare<TreeChanges>(diffModifiers, paths, explicitPathsOptions,
                new CompareOptions { Similarity = SimilarityOptions.None }))
            {
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
                                repository.Index.Remove(treeEntryChanges.Path);
                            }
                            break;

                        case ChangeKind.Deleted:
                            repository.Index.Remove(treeEntryChanges.Path);
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
                            repository.Index.Add(treeEntryChanges.Path);
                            break;

                        case ChangeKind.Conflicted:
                            if (treeEntryChanges.Exists)
                            {
                                repository.Index.Add(treeEntryChanges.Path);
                            }
                            break;

                        default:
                            continue;
                    }
                }

                repository.Index.Write();
            }
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="repository">The repository in which to act</param>
        /// <param name="path">The path of the file within the working directory.</param>
        public static void Unstage(IRepository repository, string path)
        {
            Unstage(repository, path, null);
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="repository">The repository in which to act</param>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="explicitPathsOptions">
        /// The passed <paramref name="path"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public static void Unstage(IRepository repository, string path, ExplicitPathsOptions explicitPathsOptions)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(path, "path");

            Unstage(repository, new[] { path }, explicitPathsOptions);
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a collection of file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="repository">The repository in which to act</param>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        public static void Unstage(IRepository repository, IEnumerable<string> paths)
        {
            Unstage(repository, paths, null);
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a collection of file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="repository">The repository in which to act</param>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="explicitPathsOptions">
        /// The passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public static void Unstage(IRepository repository, IEnumerable<string> paths, ExplicitPathsOptions explicitPathsOptions)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(paths, "paths");

            if (repository.Info.IsHeadUnborn)
            {
                using (var changes = repository.Diff.Compare<TreeChanges>(null, DiffTargets.Index, paths, explicitPathsOptions, new CompareOptions { Similarity = SimilarityOptions.None }))
                    repository.Index.Replace(changes);
            }
            else
            {
                repository.Index.Replace(repository.Head.Tip, paths, explicitPathsOptions);
            }

            repository.Index.Write();
        }

        /// <summary>
        /// Moves and/or renames a file in the working directory and promotes the change to the staging area.
        /// </summary>
        /// <param name="repository">The repository to act on</param>
        /// <param name="sourcePath">The path of the file within the working directory which has to be moved/renamed.</param>
        /// <param name="destinationPath">The target path of the file within the working directory.</param>
        public static void Move(IRepository repository, string sourcePath, string destinationPath)
        {
            Move(repository, new[] { sourcePath }, new[] { destinationPath });
        }

        /// <summary>
        /// Moves and/or renames a collection of files in the working directory and promotes the changes to the staging area.
        /// </summary>
        /// <param name="repository">The repository to act on</param>
        /// <param name="sourcePaths">The paths of the files within the working directory which have to be moved/renamed.</param>
        /// <param name="destinationPaths">The target paths of the files within the working directory.</param>
        public static void Move(IRepository repository, IEnumerable<string> sourcePaths, IEnumerable<string> destinationPaths)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(sourcePaths, "sourcePaths");
            Ensure.ArgumentNotNull(destinationPaths, "destinationPaths");

            //TODO: Move() should support following use cases:
            // - Moving a file under a directory ('file' and 'dir' -> 'dir/file')
            // - Moving a directory (and its content) under another directory ('dir1' and 'dir2' -> 'dir2/dir1/*')

            //TODO: Move() should throw when:
            // - Moving a directory under a file

            IDictionary<Tuple<string, FileStatus>, Tuple<string, FileStatus>> batch = PrepareBatch(repository, sourcePaths, destinationPaths);

            if (batch.Count == 0)
            {
                throw new ArgumentNullException(nameof(sourcePaths));
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

            string wd = repository.Info.WorkingDirectory;
            var index = repository.Index;
            foreach (KeyValuePair<Tuple<string, FileStatus>, Tuple<string, FileStatus>> keyValuePair in batch)
            {
                string from = keyValuePair.Key.Item1;
                string to = keyValuePair.Value.Item1;

                index.Remove(from);
                File.Move(Path.Combine(wd, from), Path.Combine(wd, to));
                index.Add(to);
            }

            index.Write();
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

        private static IDictionary<Tuple<string, FileStatus>, Tuple<string, FileStatus>> PrepareBatch(IRepository repository, IEnumerable<string> leftPaths, IEnumerable<string> rightPaths)
        {
            IDictionary<Tuple<string, FileStatus>, Tuple<string, FileStatus>> dic = new Dictionary<Tuple<string, FileStatus>, Tuple<string, FileStatus>>();

            IEnumerator<string> leftEnum = leftPaths.GetEnumerator();
            IEnumerator<string> rightEnum = rightPaths.GetEnumerator();

            while (Enumerate(leftEnum, rightEnum))
            {
                Tuple<string, FileStatus> from = BuildFrom(repository, leftEnum.Current);
                Tuple<string, FileStatus> to = BuildFrom(repository, rightEnum.Current);
                dic.Add(from, to);
            }

            return dic;
        }

        private static Tuple<string, FileStatus> BuildFrom(IRepository repository, string path)
        {
            string relativePath = repository.BuildRelativePathFrom(path);
            return new Tuple<string, FileStatus>(relativePath, repository.RetrieveStatus(relativePath));
        }
    }
}

