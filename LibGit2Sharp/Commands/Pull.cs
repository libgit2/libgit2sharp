using System;
using LibGit2Sharp;
using LibGit2Sharp.Core;

namespace LibGit2Sharp.Commands
{
    /// <summary>
    /// Fetch changes from the configured upstream remote and branch into the branch pointed at by HEAD.
    /// </summary>
    public class Pull
    {
        private readonly Repository repository;
        private readonly Signature merger;
        private readonly PullOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibGit2Sharp.Commands.Pull"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="merger">The signature to use for the merge.</param>
        /// <param name="options">The options for fetch and merging.</param>
        public Pull(Repository repository, Signature merger, PullOptions options)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(merger, "merger");
            Ensure.ArgumentNotNull(options, "options");

            this.repository = repository;
            this.merger = merger;
            this.options = options;
        }

        /// <summary>
        /// Run this command
        /// </summary>
        public MergeResult Run()
        {

            Branch currentBranch = repository.Head;

            if (!currentBranch.IsTracking)
            {
                throw new LibGit2SharpException("There is no tracking information for the current branch.");
            }

            if (currentBranch.RemoteName == null)
            {
                throw new LibGit2SharpException("No upstream remote for the current branch.");
            }

            new Commands.Fetch(repository, currentBranch.RemoteName, new string[0], options.FetchOptions, null).Run();
            return repository.MergeFetchedRefs(merger, options.MergeOptions);
        }
    }
}

