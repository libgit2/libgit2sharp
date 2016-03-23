using System;
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

            var changes = repository.Diff.Compare<TreeChanges>(diffModifiers, paths, explicitPathsOptions,
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
                var changes = repository.Diff.Compare<TreeChanges>(null, DiffTargets.Index, paths, explicitPathsOptions, new CompareOptions { Similarity = SimilarityOptions.None });

                repository.Index.Replace(changes);
            }
            else
            {
                repository.Index.Replace(repository.Head.Tip, paths, explicitPathsOptions);
            }
        }
    }
}

