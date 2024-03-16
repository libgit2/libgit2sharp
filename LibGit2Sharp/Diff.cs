using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Show changes between the working tree and the index or a tree, changes between the index and a tree, changes between two trees, or changes between two files on disk.
    /// <para>
    ///   Copied and renamed files currently cannot be detected, as the feature is not supported by libgit2 yet.
    ///   These files will be shown as a pair of Deleted/Added files.</para>
    /// </summary>
    public class Diff
    {
        private readonly Repository repo;

        private static GitDiffOptions BuildOptions(DiffModifiers diffOptions, FilePath[] filePaths = null, MatchedPathsAggregator matchedPathsAggregator = null, CompareOptions compareOptions = null)
        {
            var options = new GitDiffOptions();

            options.Flags |= GitDiffOptionFlags.GIT_DIFF_INCLUDE_TYPECHANGE;

            compareOptions = compareOptions ?? new CompareOptions();
            options.ContextLines = (ushort)compareOptions.ContextLines;
            options.InterhunkLines = (ushort)compareOptions.InterhunkLines;

            if (diffOptions.HasFlag(DiffModifiers.IncludeUntracked))
            {
                options.Flags |= GitDiffOptionFlags.GIT_DIFF_INCLUDE_UNTRACKED |
                GitDiffOptionFlags.GIT_DIFF_RECURSE_UNTRACKED_DIRS |
                GitDiffOptionFlags.GIT_DIFF_SHOW_UNTRACKED_CONTENT;
            }

            if (diffOptions.HasFlag(DiffModifiers.IncludeIgnored))
            {
                options.Flags |= GitDiffOptionFlags.GIT_DIFF_INCLUDE_IGNORED |
                GitDiffOptionFlags.GIT_DIFF_RECURSE_IGNORED_DIRS;
            }

            if (diffOptions.HasFlag(DiffModifiers.IncludeUnmodified) || compareOptions.IncludeUnmodified ||
                (compareOptions.Similarity != null &&
                 (compareOptions.Similarity.RenameDetectionMode == RenameDetectionMode.CopiesHarder ||
                  compareOptions.Similarity.RenameDetectionMode == RenameDetectionMode.Exact)))
            {
                options.Flags |= GitDiffOptionFlags.GIT_DIFF_INCLUDE_UNMODIFIED;
            }

            if (compareOptions.Algorithm == DiffAlgorithm.Patience)
            {
                options.Flags |= GitDiffOptionFlags.GIT_DIFF_PATIENCE;
            }
            else if (compareOptions.Algorithm == DiffAlgorithm.Minimal)
            {
                options.Flags |= GitDiffOptionFlags.GIT_DIFF_MINIMAL;
            }

            if (diffOptions.HasFlag(DiffModifiers.DisablePathspecMatch))
            {
                options.Flags |= GitDiffOptionFlags.GIT_DIFF_DISABLE_PATHSPEC_MATCH;
            }

            if (compareOptions.IndentHeuristic)
            {
                options.Flags |= GitDiffOptionFlags.GIT_DIFF_INDENT_HEURISTIC;
            }

            if (matchedPathsAggregator != null)
            {
                options.NotifyCallback = matchedPathsAggregator.OnGitDiffNotify;
            }

            if (filePaths != null)
            {
                options.PathSpec = GitStrArrayManaged.BuildFrom(filePaths);
            }

            return options;
        }

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Diff()
        { }

        internal Diff(Repository repo)
        {
            this.repo = repo;
        }

        private static readonly IDictionary<DiffTargets, Func<Repository, TreeComparisonHandleRetriever>> HandleRetrieverDispatcher = BuildHandleRetrieverDispatcher();

        private static IDictionary<DiffTargets, Func<Repository, TreeComparisonHandleRetriever>> BuildHandleRetrieverDispatcher()
        {
            return new Dictionary<DiffTargets, Func<Repository, TreeComparisonHandleRetriever>>
                       {
                           { DiffTargets.Index, IndexToTree },
                           { DiffTargets.WorkingDirectory, WorkdirToTree },
                           { DiffTargets.Index | DiffTargets.WorkingDirectory, WorkdirAndIndexToTree },
                       };
        }

        private static readonly IDictionary<Type, Func<DiffHandle, object>> ChangesBuilders = new Dictionary<Type, Func<DiffHandle, object>>
        {
            { typeof(Patch), diff => new Patch(diff) },
            { typeof(TreeChanges), diff => new TreeChanges(diff) },
            { typeof(PatchStats), diff => new PatchStats(diff) },
        };


        private static T BuildDiffResult<T>(DiffHandle diff) where T : class, IDiffResult
        {
            Func<DiffHandle, object> builder;

            if (!ChangesBuilders.TryGetValue(typeof(T), out builder))
            {
                throw new LibGit2SharpException("User-defined types passed to Compare are not supported. Supported values are: {0}",
                    string.Join(", ", ChangesBuilders.Keys.Select(x => x.Name)));
            }

            return (T)builder(diff);
        }

        /// <summary>
        /// Show changes between two <see cref="Blob"/>s.
        /// </summary>
        /// <param name="oldBlob">The <see cref="Blob"/> you want to compare from.</param>
        /// <param name="newBlob">The <see cref="Blob"/> you want to compare to.</param>
        /// <returns>A <see cref="ContentChanges"/> containing the changes between the <paramref name="oldBlob"/> and the <paramref name="newBlob"/>.</returns>
        public virtual ContentChanges Compare(Blob oldBlob, Blob newBlob)
        {
            return Compare(oldBlob, newBlob, null);
        }

        /// <summary>
        /// Show changes between two <see cref="Blob"/>s.
        /// </summary>
        /// <param name="oldBlob">The <see cref="Blob"/> you want to compare from.</param>
        /// <param name="newBlob">The <see cref="Blob"/> you want to compare to.</param>
        /// <param name="compareOptions">Additional options to define comparison behavior.</param>
        /// <returns>A <see cref="ContentChanges"/> containing the changes between the <paramref name="oldBlob"/> and the <paramref name="newBlob"/>.</returns>
        public virtual ContentChanges Compare(Blob oldBlob, Blob newBlob, CompareOptions compareOptions)
        {
            using (GitDiffOptions options = BuildOptions(DiffModifiers.None, compareOptions: compareOptions))
            {
                return new ContentChanges(repo, oldBlob, newBlob, options);
            }
        }

        /// <summary>
        /// Show changes between two <see cref="Tree"/>s.
        /// </summary>
        /// <param name="oldTree">The <see cref="Tree"/> you want to compare from.</param>
        /// <param name="newTree">The <see cref="Tree"/> you want to compare to.</param>
        /// <returns>A <see cref="TreeChanges"/> containing the changes between the <paramref name="oldTree"/> and the <paramref name="newTree"/>.</returns>
        public virtual T Compare<T>(Tree oldTree, Tree newTree) where T : class, IDiffResult
        {
            return Compare<T>(oldTree, newTree, null, null, null);
        }

        /// <summary>
        /// Show changes between two <see cref="Tree"/>s.
        /// </summary>
        /// <param name="oldTree">The <see cref="Tree"/> you want to compare from.</param>
        /// <param name="newTree">The <see cref="Tree"/> you want to compare to.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be compared.</param>
        /// <returns>A <see cref="TreeChanges"/> containing the changes between the <paramref name="oldTree"/> and the <paramref name="newTree"/>.</returns>
        public virtual T Compare<T>(Tree oldTree, Tree newTree, IEnumerable<string> paths) where T : class, IDiffResult
        {
            return Compare<T>(oldTree, newTree, paths, null, null);
        }

        /// <summary>
        /// Show changes between two <see cref="Tree"/>s.
        /// </summary>
        /// <param name="oldTree">The <see cref="Tree"/> you want to compare from.</param>
        /// <param name="newTree">The <see cref="Tree"/> you want to compare to.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be compared.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        /// <returns>A <see cref="TreeChanges"/> containing the changes between the <paramref name="oldTree"/> and the <paramref name="newTree"/>.</returns>
        public virtual T Compare<T>(Tree oldTree, Tree newTree, IEnumerable<string> paths,
            ExplicitPathsOptions explicitPathsOptions) where T : class, IDiffResult
        {
            return Compare<T>(oldTree, newTree, paths, explicitPathsOptions, null);
        }

        /// <summary>
        /// Show changes between two <see cref="Tree"/>s.
        /// </summary>
        /// <param name="oldTree">The <see cref="Tree"/> you want to compare from.</param>
        /// <param name="newTree">The <see cref="Tree"/> you want to compare to.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be compared.</param>
        /// <param name="compareOptions">Additional options to define patch generation behavior.</param>
        /// <returns>A <see cref="TreeChanges"/> containing the changes between the <paramref name="oldTree"/> and the <paramref name="newTree"/>.</returns>
        public virtual T Compare<T>(Tree oldTree, Tree newTree, IEnumerable<string> paths, CompareOptions compareOptions) where T : class, IDiffResult
        {
            return Compare<T>(oldTree, newTree, paths, null, compareOptions);
        }

        /// <summary>
        /// Show changes between two <see cref="Tree"/>s.
        /// </summary>
        /// <param name="oldTree">The <see cref="Tree"/> you want to compare from.</param>
        /// <param name="newTree">The <see cref="Tree"/> you want to compare to.</param>
        /// <param name="compareOptions">Additional options to define patch generation behavior.</param>
        /// <returns>A <see cref="TreeChanges"/> containing the changes between the <paramref name="oldTree"/> and the <paramref name="newTree"/>.</returns>
        public virtual T Compare<T>(Tree oldTree, Tree newTree, CompareOptions compareOptions) where T : class, IDiffResult
        {
            return Compare<T>(oldTree, newTree, null, null, compareOptions);
        }

        /// <summary>
        /// Show changes between two <see cref="Tree"/>s.
        /// </summary>
        /// <param name="oldTree">The <see cref="Tree"/> you want to compare from.</param>
        /// <param name="newTree">The <see cref="Tree"/> you want to compare to.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be compared.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        /// <param name="compareOptions">Additional options to define patch generation behavior.</param>
        /// <returns>A <see cref="TreeChanges"/> containing the changes between the <paramref name="oldTree"/> and the <paramref name="newTree"/>.</returns>
        public virtual T Compare<T>(Tree oldTree, Tree newTree, IEnumerable<string> paths, ExplicitPathsOptions explicitPathsOptions,
                               CompareOptions compareOptions) where T : class, IDiffResult
        {
            var comparer = TreeToTree(repo);
            ObjectId oldTreeId = oldTree != null ? oldTree.Id : null;
            ObjectId newTreeId = newTree != null ? newTree.Id : null;
            var diffOptions = DiffModifiers.None;

            if (explicitPathsOptions != null)
            {
                diffOptions |= DiffModifiers.DisablePathspecMatch;

                if (explicitPathsOptions.ShouldFailOnUnmatchedPath || explicitPathsOptions.OnUnmatchedPath != null)
                {
                    diffOptions |= DiffModifiers.IncludeUnmodified;
                }
            }

            DiffHandle diff = BuildDiffList(oldTreeId, newTreeId, comparer, diffOptions, paths, explicitPathsOptions, compareOptions);

            try
            {
                return BuildDiffResult<T>(diff);
            }
            catch
            {
                diff.SafeDispose();
                throw;
            }
        }

        /// <summary>
        /// Show changes between a <see cref="Tree"/> and the Index, the Working Directory, or both.
        /// <para>
        /// The level of diff performed can be specified by passing either a <see cref="TreeChanges"/>
        /// or <see cref="Patch"/> type as the generic parameter.
        /// </para>
        /// </summary>
        /// <param name="oldTree">The <see cref="Tree"/> to compare from.</param>
        /// <param name="diffTargets">The targets to compare to.</param>
        /// <typeparam name="T">Can be either a <see cref="TreeChanges"/> if you are only interested in the list of files modified, added, ..., or
        /// a <see cref="Patch"/> if you want the actual patch content for the whole diff and for individual files.</typeparam>
        /// <returns>A <typeparamref name="T"/> containing the changes between the <see cref="Tree"/> and the selected target.</returns>
        public virtual T Compare<T>(Tree oldTree, DiffTargets diffTargets) where T : class, IDiffResult
        {
            return Compare<T>(oldTree, diffTargets, null, null, null);
        }

        /// <summary>
        /// Show changes between a <see cref="Tree"/> and the Index, the Working Directory, or both.
        /// <para>
        /// The level of diff performed can be specified by passing either a <see cref="TreeChanges"/>
        /// or <see cref="Patch"/> type as the generic parameter.
        /// </para>
        /// </summary>
        /// <param name="oldTree">The <see cref="Tree"/> to compare from.</param>
        /// <param name="diffTargets">The targets to compare to.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be compared.</param>
        /// <typeparam name="T">Can be either a <see cref="TreeChanges"/> if you are only interested in the list of files modified, added, ..., or
        /// a <see cref="Patch"/> if you want the actual patch content for the whole diff and for individual files.</typeparam>
        /// <returns>A <typeparamref name="T"/> containing the changes between the <see cref="Tree"/> and the selected target.</returns>
        public virtual T Compare<T>(Tree oldTree, DiffTargets diffTargets, IEnumerable<string> paths) where T : class, IDiffResult
        {
            return Compare<T>(oldTree, diffTargets, paths, null, null);
        }

        /// <summary>
        /// Show changes between a <see cref="Tree"/> and the Index, the Working Directory, or both.
        /// <para>
        /// The level of diff performed can be specified by passing either a <see cref="TreeChanges"/>
        /// or <see cref="Patch"/> type as the generic parameter.
        /// </para>
        /// </summary>
        /// <param name="oldTree">The <see cref="Tree"/> to compare from.</param>
        /// <param name="diffTargets">The targets to compare to.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be compared.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        /// <typeparam name="T">Can be either a <see cref="TreeChanges"/> if you are only interested in the list of files modified, added, ..., or
        /// a <see cref="Patch"/> if you want the actual patch content for the whole diff and for individual files.</typeparam>
        /// <returns>A <typeparamref name="T"/> containing the changes between the <see cref="Tree"/> and the selected target.</returns>
        public virtual T Compare<T>(Tree oldTree, DiffTargets diffTargets, IEnumerable<string> paths,
            ExplicitPathsOptions explicitPathsOptions) where T : class, IDiffResult
        {
            return Compare<T>(oldTree, diffTargets, paths, explicitPathsOptions, null);
        }

        /// <summary>
        /// Show changes between a <see cref="Tree"/> and the Index, the Working Directory, or both.
        /// <para>
        /// The level of diff performed can be specified by passing either a <see cref="TreeChanges"/>
        /// or <see cref="Patch"/> type as the generic parameter.
        /// </para>
        /// </summary>
        /// <param name="oldTree">The <see cref="Tree"/> to compare from.</param>
        /// <param name="diffTargets">The targets to compare to.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be compared.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        /// <param name="compareOptions">Additional options to define patch generation behavior.</param>
        /// <typeparam name="T">Can be either a <see cref="TreeChanges"/> if you are only interested in the list of files modified, added, ..., or
        /// a <see cref="Patch"/> if you want the actual patch content for the whole diff and for individual files.</typeparam>
        /// <returns>A <typeparamref name="T"/> containing the changes between the <see cref="Tree"/> and the selected target.</returns>
        public virtual T Compare<T>(Tree oldTree, DiffTargets diffTargets, IEnumerable<string> paths,
            ExplicitPathsOptions explicitPathsOptions, CompareOptions compareOptions) where T : class, IDiffResult
        {
            var comparer = HandleRetrieverDispatcher[diffTargets](repo);
            ObjectId oldTreeId = oldTree != null ? oldTree.Id : null;

            DiffModifiers diffOptions = diffTargets.HasFlag(DiffTargets.WorkingDirectory)
                ? DiffModifiers.IncludeUntracked
                : DiffModifiers.None;

            if (explicitPathsOptions != null)
            {
                diffOptions |= DiffModifiers.DisablePathspecMatch;

                if (explicitPathsOptions.ShouldFailOnUnmatchedPath || explicitPathsOptions.OnUnmatchedPath != null)
                {
                    diffOptions |= DiffModifiers.IncludeUnmodified;
                }
            }

            DiffHandle diff = BuildDiffList(oldTreeId, null, comparer, diffOptions, paths, explicitPathsOptions, compareOptions);

            try
            {
                return BuildDiffResult<T>(diff);
            }
            catch
            {
                diff.SafeDispose();
                throw;
            }
        }

        /// <summary>
        /// Show changes between the working directory and the index.
        /// <para>
        /// The level of diff performed can be specified by passing either a <see cref="TreeChanges"/>
        /// or <see cref="Patch"/> type as the generic parameter.
        /// </para>
        /// </summary>
        /// <typeparam name="T">Can be either a <see cref="TreeChanges"/> if you are only interested in the list of files modified, added, ..., or
        /// a <see cref="Patch"/> if you want the actual patch content for the whole diff and for individual files.</typeparam>
        /// <returns>A <typeparamref name="T"/> containing the changes between the working directory and the index.</returns>
        public virtual T Compare<T>() where T : class, IDiffResult
        {
            return Compare<T>(DiffModifiers.None);
        }

        /// <summary>
        /// Show changes between the working directory and the index.
        /// <para>
        /// The level of diff performed can be specified by passing either a <see cref="TreeChanges"/>
        /// or <see cref="Patch"/> type as the generic parameter.
        /// </para>
        /// </summary>
        /// <param name="paths">The list of paths (either files or directories) that should be compared.</param>
        /// <typeparam name="T">Can be either a <see cref="TreeChanges"/> if you are only interested in the list of files modified, added, ..., or
        /// a <see cref="Patch"/> if you want the actual patch content for the whole diff and for individual files.</typeparam>
        /// <returns>A <typeparamref name="T"/> containing the changes between the working directory and the index.</returns>
        public virtual T Compare<T>(IEnumerable<string> paths) where T : class, IDiffResult
        {
            return Compare<T>(DiffModifiers.None, paths);
        }

        /// <summary>
        /// Show changes between the working directory and the index.
        /// <para>
        /// The level of diff performed can be specified by passing either a <see cref="TreeChanges"/>
        /// or <see cref="Patch"/> type as the generic parameter.
        /// </para>
        /// </summary>
        /// <param name="paths">The list of paths (either files or directories) that should be compared.</param>
        /// <param name="includeUntracked">If true, include untracked files from the working dir as additions. Otherwise ignore them.</param>
        /// <typeparam name="T">Can be either a <see cref="TreeChanges"/> if you are only interested in the list of files modified, added, ..., or
        /// a <see cref="Patch"/> if you want the actual patch content for the whole diff and for individual files.</typeparam>
        /// <returns>A <typeparamref name="T"/> containing the changes between the working directory and the index.</returns>
        public virtual T Compare<T>(IEnumerable<string> paths, bool includeUntracked) where T : class, IDiffResult
        {
            return Compare<T>(includeUntracked ? DiffModifiers.IncludeUntracked : DiffModifiers.None, paths);
        }

        /// <summary>
        /// Show changes between the working directory and the index.
        /// <para>
        /// The level of diff performed can be specified by passing either a <see cref="TreeChanges"/>
        /// or <see cref="Patch"/> type as the generic parameter.
        /// </para>
        /// </summary>
        /// <param name="paths">The list of paths (either files or directories) that should be compared.</param>
        /// <param name="includeUntracked">If true, include untracked files from the working dir as additions. Otherwise ignore them.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        /// <typeparam name="T">Can be either a <see cref="TreeChanges"/> if you are only interested in the list of files modified, added, ..., or
        /// a <see cref="Patch"/> if you want the actual patch content for the whole diff and for individual files.</typeparam>
        /// <returns>A <typeparamref name="T"/> containing the changes between the working directory and the index.</returns>
        public virtual T Compare<T>(IEnumerable<string> paths, bool includeUntracked, ExplicitPathsOptions explicitPathsOptions) where T : class, IDiffResult
        {
            return Compare<T>(includeUntracked ? DiffModifiers.IncludeUntracked : DiffModifiers.None, paths, explicitPathsOptions);
        }

        /// <summary>
        /// Show changes between the working directory and the index.
        /// <para>
        /// The level of diff performed can be specified by passing either a <see cref="TreeChanges"/>
        /// or <see cref="Patch"/> type as the generic parameter.
        /// </para>
        /// </summary>
        /// <param name="paths">The list of paths (either files or directories) that should be compared.</param>
        /// <param name="includeUntracked">If true, include untracked files from the working dir as additions. Otherwise ignore them.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        /// <param name="compareOptions">Additional options to define patch generation behavior.</param>
        /// <typeparam name="T">Can be either a <see cref="TreeChanges"/> if you are only interested in the list of files modified, added, ..., or
        /// a <see cref="Patch"/> if you want the actual patch content for the whole diff and for individual files.</typeparam>
        /// <returns>A <typeparamref name="T"/> containing the changes between the working directory and the index.</returns>
        public virtual T Compare<T>(
            IEnumerable<string> paths,
            bool includeUntracked,
            ExplicitPathsOptions explicitPathsOptions,
            CompareOptions compareOptions) where T : class, IDiffResult
        {
            return Compare<T>(includeUntracked ? DiffModifiers.IncludeUntracked : DiffModifiers.None, paths, explicitPathsOptions, compareOptions);
        }

        internal virtual T Compare<T>(
            DiffModifiers diffOptions,
            IEnumerable<string> paths = null,
            ExplicitPathsOptions explicitPathsOptions = null,
            CompareOptions compareOptions = null) where T : class, IDiffResult
        {
            var comparer = WorkdirToIndex(repo);

            if (explicitPathsOptions != null)
            {
                diffOptions |= DiffModifiers.DisablePathspecMatch;

                if (explicitPathsOptions.ShouldFailOnUnmatchedPath || explicitPathsOptions.OnUnmatchedPath != null)
                {
                    diffOptions |= DiffModifiers.IncludeUnmodified;
                }
            }

            DiffHandle diff = BuildDiffList(null, null, comparer, diffOptions, paths, explicitPathsOptions, compareOptions);

            try
            {
                return BuildDiffResult<T>(diff);
            }
            catch
            {
                diff.SafeDispose();
                throw;
            }
        }

        internal delegate DiffHandle TreeComparisonHandleRetriever(ObjectId oldTreeId, ObjectId newTreeId, GitDiffOptions options);

        private static TreeComparisonHandleRetriever TreeToTree(Repository repo)
        {
            return (oh, nh, o) => Proxy.git_diff_tree_to_tree(repo.Handle, oh, nh, o);
        }

        private static TreeComparisonHandleRetriever WorkdirToIndex(Repository repo)
        {
            return (oh, nh, o) => Proxy.git_diff_index_to_workdir(repo.Handle, repo.Index.Handle, o);
        }

        private static TreeComparisonHandleRetriever WorkdirToTree(Repository repo)
        {
            return (oh, nh, o) => Proxy.git_diff_tree_to_workdir(repo.Handle, oh, o);
        }

        private static TreeComparisonHandleRetriever WorkdirAndIndexToTree(Repository repo)
        {
            TreeComparisonHandleRetriever comparisonHandleRetriever = (oh, nh, o) =>
            {
                DiffHandle diff = Proxy.git_diff_tree_to_index(repo.Handle, repo.Index.Handle, oh, o);

                using (DiffHandle diff2 = Proxy.git_diff_index_to_workdir(repo.Handle, repo.Index.Handle, o))
                {
                    Proxy.git_diff_merge(diff, diff2);
                }

                return diff;
            };

            return comparisonHandleRetriever;
        }

        private static TreeComparisonHandleRetriever IndexToTree(Repository repo)
        {
            return (oh, nh, o) => Proxy.git_diff_tree_to_index(repo.Handle, repo.Index.Handle, oh, o);
        }

        private DiffHandle BuildDiffList(
            ObjectId oldTreeId,
            ObjectId newTreeId,
            TreeComparisonHandleRetriever comparisonHandleRetriever,
            DiffModifiers diffOptions,
            IEnumerable<string> paths,
            ExplicitPathsOptions explicitPathsOptions,
            CompareOptions compareOptions)
        {
            var filePaths = repo.ToFilePaths(paths);

            MatchedPathsAggregator matchedPaths = null;

            // We can't match paths unless we've got something to match
            // against and we're told to do so.
            if (filePaths != null && explicitPathsOptions != null)
            {
                if (explicitPathsOptions.OnUnmatchedPath != null || explicitPathsOptions.ShouldFailOnUnmatchedPath)
                {
                    matchedPaths = new MatchedPathsAggregator();
                }
            }

            using (GitDiffOptions options = BuildOptions(diffOptions, filePaths, matchedPaths, compareOptions))
            {
                var diffList = comparisonHandleRetriever(oldTreeId, newTreeId, options);

                if (matchedPaths != null)
                {
                    try
                    {
                        DispatchUnmatchedPaths(explicitPathsOptions, filePaths, matchedPaths);
                    }
                    catch
                    {
                        diffList.Dispose();
                        throw;
                    }
                }

                DetectRenames(diffList, compareOptions);

                return diffList;
            }
        }

        private static void DetectRenames(DiffHandle diffList, CompareOptions compareOptions)
        {
            var similarityOptions = (compareOptions == null) ? null : compareOptions.Similarity;
            if (similarityOptions == null || similarityOptions.RenameDetectionMode == RenameDetectionMode.Default)
            {
                Proxy.git_diff_find_similar(diffList, null);
                return;
            }

            if (similarityOptions.RenameDetectionMode == RenameDetectionMode.None)
            {
                return;
            }

            var opts = new GitDiffFindOptions
            {
                RenameThreshold = (ushort)similarityOptions.RenameThreshold,
                RenameFromRewriteThreshold = (ushort)similarityOptions.RenameFromRewriteThreshold,
                CopyThreshold = (ushort)similarityOptions.CopyThreshold,
                BreakRewriteThreshold = (ushort)similarityOptions.BreakRewriteThreshold,
                RenameLimit = (UIntPtr)similarityOptions.RenameLimit,
            };

            switch (similarityOptions.RenameDetectionMode)
            {
                case RenameDetectionMode.Exact:
                    opts.Flags = GitDiffFindFlags.GIT_DIFF_FIND_EXACT_MATCH_ONLY |
                                 GitDiffFindFlags.GIT_DIFF_FIND_RENAMES |
                                 GitDiffFindFlags.GIT_DIFF_FIND_COPIES |
                                 GitDiffFindFlags.GIT_DIFF_FIND_COPIES_FROM_UNMODIFIED;
                    break;
                case RenameDetectionMode.Renames:
                    opts.Flags = GitDiffFindFlags.GIT_DIFF_FIND_RENAMES;
                    break;
                case RenameDetectionMode.Copies:
                    opts.Flags = GitDiffFindFlags.GIT_DIFF_FIND_RENAMES |
                                 GitDiffFindFlags.GIT_DIFF_FIND_COPIES;
                    break;
                case RenameDetectionMode.CopiesHarder:
                    opts.Flags = GitDiffFindFlags.GIT_DIFF_FIND_RENAMES |
                                 GitDiffFindFlags.GIT_DIFF_FIND_COPIES |
                                 GitDiffFindFlags.GIT_DIFF_FIND_COPIES_FROM_UNMODIFIED;
                    break;
            }

            if (!compareOptions.IncludeUnmodified)
            {
                opts.Flags |= GitDiffFindFlags.GIT_DIFF_FIND_REMOVE_UNMODIFIED;
            }

            switch (similarityOptions.WhitespaceMode)
            {
                case WhitespaceMode.DontIgnoreWhitespace:
                    opts.Flags |= GitDiffFindFlags.GIT_DIFF_FIND_DONT_IGNORE_WHITESPACE;
                    break;
                case WhitespaceMode.IgnoreLeadingWhitespace:
                    opts.Flags |= GitDiffFindFlags.GIT_DIFF_FIND_IGNORE_LEADING_WHITESPACE;
                    break;
                case WhitespaceMode.IgnoreAllWhitespace:
                    opts.Flags |= GitDiffFindFlags.GIT_DIFF_FIND_IGNORE_WHITESPACE;
                    break;
            }

            Proxy.git_diff_find_similar(diffList, opts);
        }

        private static void DispatchUnmatchedPaths(
            ExplicitPathsOptions explicitPathsOptions,
            IEnumerable<FilePath> filePaths,
            IEnumerable<FilePath> matchedPaths)
        {
            List<FilePath> unmatchedPaths = (filePaths != null ?
                filePaths.Except(matchedPaths) : Enumerable.Empty<FilePath>()).ToList();

            if (unmatchedPaths.Count == 0)
            {
                return;
            }

            if (explicitPathsOptions.OnUnmatchedPath != null)
            {
                unmatchedPaths.ForEach(filePath => explicitPathsOptions.OnUnmatchedPath(filePath.Native));
            }

            if (explicitPathsOptions.ShouldFailOnUnmatchedPath)
            {
                throw new UnmatchedPathException(BuildUnmatchedPathsMessage(unmatchedPaths));
            }
        }

        private static string BuildUnmatchedPathsMessage(List<FilePath> unmatchedPaths)
        {
            var message = new StringBuilder("There were some unmatched paths:" + Environment.NewLine);
            unmatchedPaths.ForEach(filePath => message.AppendFormat("- {0}{1}", filePath.Native, Environment.NewLine));

            return message.ToString();
        }
    }
}
