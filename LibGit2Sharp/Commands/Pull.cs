using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Fetch changes from the configured upstream remote and branch into the branch pointed at by HEAD.
    /// </summary>
    public static partial class Commands
    {
        /// <summary>
        /// Fetch changes from the configured upstream remote and branch into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="merger">The signature to use for the merge.</param>
        /// <param name="options">The options for fetch and merging.</param>
        public static MergeResult Pull(Repository repository, Signature merger, PullOptions options)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(merger, "merger");


            options = options ?? new PullOptions();
            Branch currentBranch = repository.Head;

            if (!currentBranch.IsTracking)
            {
                throw new LibGit2SharpException("There is no tracking information for the current branch.");
            }

            if (currentBranch.RemoteName == null)
            {
                throw new LibGit2SharpException("No upstream remote for the current branch.");
            }

            Commands.Fetch(repository, currentBranch.RemoteName, Array.Empty<string>(), options.FetchOptions, null);
            return repository.MergeFetchedRefs(merger, options.MergeOptions);
        }
    }
}

