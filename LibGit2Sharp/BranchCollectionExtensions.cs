using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides helper overloads to a <see cref="BranchCollection"/>.
    /// </summary>
    public static class BranchCollectionExtensions
    {
        /// <summary>
        /// Create a new local branch with the specified name
        /// </summary>
        /// <param name="branches">The <see cref="BranchCollection"/> being worked with.</param>
        /// <param name="name">The name of the branch.</param>
        /// <param name="commit">The target commit.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public static Branch Add(this BranchCollection branches, string name, Commit commit)
        {
            return branches.Add(name, commit, false);
        }

        /// <summary>
        /// Create a new local branch with the specified name
        /// </summary>
        /// <param name="branches">The <see cref="BranchCollection"/> being worked with.</param>
        /// <param name="name">The name of the branch.</param>
        /// <param name="commit">The target commit.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public static Branch Add(this BranchCollection branches, string name, Commit commit, bool allowOverwrite)
        {
            Ensure.ArgumentNotNull(commit, "commit");

            return branches.Add(name, commit.Sha, allowOverwrite);
        }

        /// <summary>
        /// Deletes the branch with the specified name.
        /// </summary>
        /// <param name="name">The name of the branch to delete.</param>
        /// <param name="branches">The <see cref="BranchCollection"/> being worked with.</param>
        public static void Remove(this BranchCollection branches, string name)
        {
           branches.Remove(name, false);
        }

        /// <summary>
        /// Deletes the branch with the specified name.
        /// </summary>
        /// <param name="name">The name of the branch to delete.</param>
        /// <param name="isRemote">True if the provided <paramref name="name"/> is the name of a remote branch, false otherwise.</param>
        /// <param name="branches">The <see cref="BranchCollection"/> being worked with.</param>
        public static void Remove(this BranchCollection branches, string name, bool isRemote)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            string branchName = isRemote ? Reference.RemoteTrackingBranchPrefix + name : name;

            Branch branch = branches[branchName];

            if (branch == null)
            {
                return;
            }

            branches.Remove(branch);
        }

        /// <summary>
        /// Rename an existing local branch, using the default reflog message
        /// </summary>
        /// <param name="currentName">The current branch name.</param>
        /// <param name="newName">The new name the existing branch should bear.</param>
        /// <param name="branches">The <see cref="BranchCollection"/> being worked with.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public static Branch Rename(this BranchCollection branches, string currentName, string newName)
        {
            return branches.Rename(currentName, newName, false);
        }

        /// <summary>
        /// Rename an existing local branch, using the default reflog message
        /// </summary>
        /// <param name="currentName">The current branch name.</param>
        /// <param name="newName">The new name the existing branch should bear.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <param name="branches">The <see cref="BranchCollection"/> being worked with.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public static Branch Rename(this BranchCollection branches, string currentName, string newName, bool allowOverwrite)
        {
            Ensure.ArgumentNotNullOrEmptyString(currentName, "currentName");
            Ensure.ArgumentNotNullOrEmptyString(newName, "newName");

            Branch branch = branches[currentName];

            if (branch == null)
            {
                throw new LibGit2SharpException("No branch named '{0}' exists in the repository.");
            }

            return branches.Rename(branch, newName, allowOverwrite);
        }
    }
}
