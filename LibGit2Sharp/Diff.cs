using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Show changes between the working tree and the index or a tree, changes between the index and a tree, changes between two trees, or changes between two files on disk.
    ///   <para>Copied and renamed files currently cannot be detected, as the feature is not supported by libgit2 yet.
    ///   These files will be shown as a pair of Deleted/Added files.</para>
    /// </summary>
    public class Diff
    {
        private readonly Repository repo;

        internal Diff(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Show changes between two trees.
        /// </summary>
        /// <param name = "oldTree">The <see cref = "Tree"/> you want to compare from.</param>
        /// <param name = "newTree">The <see cref = "Tree"/> you want to compare to.</param>
        /// <returns>A <see cref = "TreeChanges"/> containing the changes between the <paramref name = "oldTree"/> and the <paramref name = "newTree"/>.</returns>
        public TreeChanges Compare(Tree oldTree, Tree newTree)
        {
            using (DiffListSafeHandle diff = BuildDiffListFromTrees(oldTree.Id, newTree.Id))
            {
                return new TreeChanges(diff);
            }
        }

        private DiffListSafeHandle BuildDiffListFromTrees(ObjectId oldTree, ObjectId newTree)
        {
            using (var osw1 = new ObjectSafeWrapper(oldTree, repo))
            using (var osw2 = new ObjectSafeWrapper(newTree, repo))
            {
                DiffListSafeHandle diff;
                GitDiffOptions options = BuildDefaultOptions();
                Ensure.Success(NativeMethods.git_diff_tree_to_tree(repo.Handle, options, osw1.ObjectPtr, osw2.ObjectPtr, out diff));

                return diff;
            }
        }

        private GitDiffOptions BuildDefaultOptions()
        {
            return new GitDiffOptions { InterhunkLines = 2 };
        }
    }
}
