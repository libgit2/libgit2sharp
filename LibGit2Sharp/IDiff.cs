namespace LibGit2Sharp
{
    public interface IDiff
    {
        /// <summary>
        ///   Show changes between two <see cref = "Tree"/>s.
        /// </summary>
        /// <param name = "oldTree">The <see cref = "Tree"/> you want to compare from.</param>
        /// <param name = "newTree">The <see cref = "Tree"/> you want to compare to.</param>
        /// <returns>A <see cref = "TreeChanges"/> containing the changes between the <paramref name = "oldTree"/> and the <paramref name = "newTree"/>.</returns>
        TreeChanges Compare(Tree oldTree, Tree newTree);

        /// <summary>
        ///   Show changes between two <see cref = "Blob"/>s.
        /// </summary>
        /// <param name = "oldBlob">The <see cref = "Blob"/> you want to compare from.</param>
        /// <param name = "newBlob">The <see cref = "Blob"/> you want to compare to.</param>
        /// <returns>A <see cref = "ContentChanges"/> containing the changes between the <paramref name = "oldBlob"/> and the <paramref name = "newBlob"/>.</returns>
        ContentChanges Compare(IBlob oldBlob, IBlob newBlob);

        /// <summary>
        ///   Show changes between a <see cref = "Tree"/> and a selectable target.
        /// </summary>
        /// <param name = "oldTree">The <see cref = "Tree"/> to compare from.</param>
        /// <param name = "diffTarget">The target to compare to.</param>
        /// <returns>A <see cref = "TreeChanges"/> containing the changes between the <see cref="Tree"/> and the selected target.</returns>
        TreeChanges Compare(Tree oldTree, DiffTarget diffTarget);
    }
}