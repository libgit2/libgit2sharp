using System;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public static class CommitCollectionExtensions
    {
        /// <summary>
        ///   Starts enumerating the <paramref name="commitCollection"/> at the specified branch.
        /// </summary>
        /// <param name="commitCollection">The commit collection to enumerate.</param>
        /// <param name = "branch">The branch.</param>
        /// <returns></returns>
        public static CommitCollection StartingAt(this CommitCollection commitCollection, Branch branch)
        {
            Ensure.ArgumentNotNull(branch, "branch");

            Commit commit = branch.Tip;

            if (commit == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "No valid object identified as '{0}' has been found in the repository.", branch.CanonicalName), "branch");
            }

            return commitCollection.StartingAt(commit.Sha);
        }

        /// <summary>
        ///   Starts enumerating the <paramref name="commitCollection"/> at the specified reference.
        /// </summary>
        /// <param name="commitCollection">The commit collection to enumerate.</param>
        /// <param name = "reference">The reference.</param>
        /// <returns></returns>
        public static CommitCollection StartingAt(this CommitCollection commitCollection, Reference reference)
        {
            Ensure.ArgumentNotNull(reference, "reference");

            return commitCollection.StartingAt(reference.CanonicalName);
        }
    }
}
