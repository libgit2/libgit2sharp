using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Show changes between the working tree and the index or a tree, changes between the index and a tree, changes between two trees, or changes between two files on disk.
    ///   <para>
    ///     Copied and renamed files currently cannot be detected, as the feature is not supported by libgit2 yet.
    ///     These files will be shown as a pair of Deleted/Added files.</para>
    /// </summary>
    public class Diff
    {
        private readonly Repository repo;

        private GitDiffOptions BuildOptions(DiffOptions diffOptions, IEnumerable<string> paths = null)
        {
            var options = new GitDiffOptions();

            if (diffOptions.HasFlag(DiffOptions.IncludeUntracked))
            {
                options.Flags |= GitDiffOptionFlags.GIT_DIFF_INCLUDE_UNTRACKED |
                GitDiffOptionFlags.GIT_DIFF_RECURSE_UNTRACKED_DIRS |
                GitDiffOptionFlags.GIT_DIFF_INCLUDE_UNTRACKED_CONTENT;
            }

            if (paths == null)
            {
                return options;
            }

            options.PathSpec = GitStrArrayIn.BuildFrom(ToFilePaths(repo, paths));
            return options;
        }

        private static FilePath[] ToFilePaths(Repository repo, IEnumerable<string> paths)
        {
            var filePaths = new List<FilePath>();

            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new ArgumentException("At least one provided path is either null or empty.", "paths");
                }

                filePaths.Add(BuildRelativePathFrom(repo, path));
            }

            if (filePaths.Count == 0)
            {
                throw new ArgumentException("No path has been provided.", "paths");
            }

            return filePaths.ToArray();
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
                                                          "Unable to process file '{0}'. This absolute filepath escapes out of the working directory of the repository ('{1}').",
                                                          normalizedPath, repo.Info.WorkingDirectory));
            }

            return normalizedPath.Substring(repo.Info.WorkingDirectory.Length);
        }

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Diff()
        { }

        internal Diff(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Show changes between two <see cref = "Tree"/>s.
        /// </summary>
        /// <param name = "oldTree">The <see cref = "Tree"/> you want to compare from.</param>
        /// <param name = "newTree">The <see cref = "Tree"/> you want to compare to.</param>
        /// <param name = "paths">The list of paths (either files or directories) that should be compared.</param>
        /// <returns>A <see cref = "TreeChanges"/> containing the changes between the <paramref name = "oldTree"/> and the <paramref name = "newTree"/>.</returns>
        public virtual TreeChanges Compare(Tree oldTree, Tree newTree, IEnumerable<string> paths = null)
        {
            using(GitDiffOptions options = BuildOptions(DiffOptions.None, paths))
            using (DiffListSafeHandle diff = BuildDiffListFromTrees(
                oldTree != null ? oldTree.Id : null,
                newTree != null ? newTree.Id : null,
                options))
            {
                return new TreeChanges(diff);
            }
        }

        private DiffListSafeHandle BuildDiffListFromTrees(ObjectId oldTree, ObjectId newTree, GitDiffOptions options)
        {
            return Proxy.git_diff_tree_to_tree(repo.Handle, oldTree, newTree, options);
        }

        /// <summary>
        ///   Show changes between two <see cref = "Blob"/>s.
        /// </summary>
        /// <param name = "oldBlob">The <see cref = "Blob"/> you want to compare from.</param>
        /// <param name = "newBlob">The <see cref = "Blob"/> you want to compare to.</param>
        /// <returns>A <see cref = "ContentChanges"/> containing the changes between the <paramref name = "oldBlob"/> and the <paramref name = "newBlob"/>.</returns>
        public virtual ContentChanges Compare(Blob oldBlob, Blob newBlob)
        {
            using (GitDiffOptions options = BuildOptions(DiffOptions.None))
            {
                return new ContentChanges(repo, oldBlob, newBlob, options);
            }
        }

        private readonly IDictionary<DiffTargets, Func<Repository, TreeComparisonHandleRetriever>> handleRetrieverDispatcher = BuildHandleRetrieverDispatcher();

        private static IDictionary<DiffTargets, Func<Repository, TreeComparisonHandleRetriever>> BuildHandleRetrieverDispatcher()
        {
            return new Dictionary<DiffTargets, Func<Repository, TreeComparisonHandleRetriever>>
                       {
                           { DiffTargets.Index, IndexToTree },
                           { DiffTargets.WorkingDirectory, WorkdirToTree },
                           { DiffTargets.Index | DiffTargets.WorkingDirectory, WorkdirAndIndexToTree },
                       };
        }

        /// <summary>
        ///   Show changes between a <see cref = "Tree"/> and a selectable target.
        /// </summary>
        /// <param name = "oldTree">The <see cref = "Tree"/> to compare from.</param>
        /// <param name = "diffTarget">The target to compare to.</param>
        /// <param name = "paths">The list of paths (either files or directories) that should be compared.</param>
        /// <returns>A <see cref = "TreeChanges"/> containing the changes between the <see cref="Tree"/> and the selected target.</returns>
        [Obsolete("This method will be removed in the next release. Please use Compare(Tree, Tree, DiffTargets, IEnumerable<string>) instead.")]
        public virtual TreeChanges Compare(Tree oldTree, DiffTarget diffTarget, IEnumerable<string> paths = null)
        {
            DiffTargets targets;

            switch (diffTarget)
            {
                case DiffTarget.Index:
                    targets = DiffTargets.Index;
                    break;

                case DiffTarget.WorkingDirectory:
                    targets = DiffTargets.WorkingDirectory;
                    break;

                default:
                    targets = DiffTargets.Index | DiffTargets.WorkingDirectory;
                    break;
            }

            return Compare(oldTree, targets, paths);
        }

        /// <summary>
        ///   Show changes between a <see cref = "Tree"/> and the Index, the Working Directory, or both.
        /// </summary>
        /// <param name = "oldTree">The <see cref = "Tree"/> to compare from.</param>
        /// <param name = "diffTargets">The targets to compare to.</param>
        /// <param name = "paths">The list of paths (either files or directories) that should be compared.</param>
        /// <returns>A <see cref = "TreeChanges"/> containing the changes between the <see cref="Tree"/> and the selected target.</returns>
        public virtual TreeChanges Compare(Tree oldTree, DiffTargets diffTargets, IEnumerable<string> paths = null)
        {
            var comparer = handleRetrieverDispatcher[diffTargets](repo);

            DiffOptions diffOptions = diffTargets.HasFlag(DiffTargets.WorkingDirectory) ?
                DiffOptions.IncludeUntracked : DiffOptions.None;

            using (GitDiffOptions options = BuildOptions(diffOptions, paths))
            using (DiffListSafeHandle dl = BuildDiffListFromTreeAndComparer(
                oldTree != null ? oldTree.Id : null,
                comparer, options))
            {
                return new TreeChanges(dl);
            }
        }

        /// <summary>
        ///   Show changes between the working directory and the index.
        /// </summary>
        /// <param name = "paths">The list of paths (either files or directories) that should be compared.</param>
        /// <returns>A <see cref = "TreeChanges"/> containing the changes between the working directory and the index.</returns>
        public virtual TreeChanges Compare(IEnumerable<string> paths = null)
        {
            var comparer = WorkdirToIndex(repo);

            using (GitDiffOptions options = BuildOptions(DiffOptions.None, paths))
            using (DiffListSafeHandle dl = BuildDiffListFromComparer(null, comparer, options))
            {
                return new TreeChanges(dl);
            }
        }

        private delegate DiffListSafeHandle TreeComparisonHandleRetriever(ObjectId id, GitDiffOptions options);

        private static TreeComparisonHandleRetriever WorkdirToIndex(Repository repo)
        {
            return (h, o) => Proxy.git_diff_index_to_workdir(repo.Handle, repo.Index.Handle, o);
        }

        private static TreeComparisonHandleRetriever WorkdirToTree(Repository repo)
        {
            return (h, o) => Proxy.git_diff_tree_to_workdir(repo.Handle, h, o);
        }

        private static TreeComparisonHandleRetriever WorkdirAndIndexToTree(Repository repo)
        {
            TreeComparisonHandleRetriever comparisonHandleRetriever = (h, o) =>
            {
                DiffListSafeHandle diff = null, diff2 = null;

                try
                {
                    diff = Proxy.git_diff_tree_to_index(repo.Handle, repo.Index.Handle, h, o);
                    diff2 = Proxy.git_diff_index_to_workdir(repo.Handle, repo.Index.Handle, o);
                    Proxy.git_diff_merge(diff, diff2);
                }
                catch
                {
                    diff.SafeDispose();
                    throw;
                }
                finally
                {
                    diff2.SafeDispose();
                }

                return diff;
            };

            return comparisonHandleRetriever;
        }

        private static TreeComparisonHandleRetriever IndexToTree(Repository repo)
        {
            return (h, o) => Proxy.git_diff_tree_to_index(repo.Handle, repo.Index.Handle, h, o);
        }

        private static DiffListSafeHandle BuildDiffListFromTreeAndComparer(ObjectId treeId, TreeComparisonHandleRetriever comparisonHandleRetriever, GitDiffOptions options)
        {
            return BuildDiffListFromComparer(treeId, comparisonHandleRetriever, options);
        }

        private static DiffListSafeHandle BuildDiffListFromComparer(ObjectId treeId, TreeComparisonHandleRetriever comparisonHandleRetriever, GitDiffOptions options)
        {
            return comparisonHandleRetriever(treeId, options);
        }
    }
}
