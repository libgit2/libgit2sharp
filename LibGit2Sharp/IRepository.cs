using System;
using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    /// A Repository is the primary interface into a git repository
    /// </summary>
    public interface IRepository : IDisposable
    {
        /// <summary>
        /// Shortcut to return the branch pointed to by HEAD
        /// </summary>
        Branch Head { get; }

        /// <summary>
        /// Provides access to the configuration settings for this repository.
        /// </summary>
        Configuration Config { get; }

        /// <summary>
        /// Gets the index.
        /// </summary>
        Index Index { get; }

        /// <summary>
        /// Lookup and enumerate references in the repository.
        /// </summary>
        ReferenceCollection Refs { get; }

        /// <summary>
        /// Lookup and enumerate commits in the repository.
        /// Iterating this collection directly starts walking from the HEAD.
        /// </summary>
        IQueryableCommitLog Commits { get; }

        /// <summary>
        /// Lookup and enumerate branches in the repository.
        /// </summary>
        BranchCollection Branches { get; }

        /// <summary>
        /// Lookup and enumerate tags in the repository.
        /// </summary>
        TagCollection Tags { get; }

        /// <summary>
        /// Provides high level information about this repository.
        /// </summary>
        RepositoryInformation Info { get; }

        /// <summary>
        /// Provides access to diffing functionalities to show changes between the working tree and the index or a tree, changes between the index and a tree, changes between two trees, or changes between two files on disk.
        /// </summary>
        Diff Diff { get; }

        /// <summary>
        /// Gets the database.
        /// </summary>
        ObjectDatabase ObjectDatabase { get; }

        /// <summary>
        /// Lookup notes in the repository.
        /// </summary>
        NoteCollection Notes { get; }

        /// <summary>
        /// Submodules in the repository.
        /// </summary>
        SubmoduleCollection Submodules { get; }

        /// <summary>
        /// Checkout the commit pointed at by the tip of the specified <see cref="Branch"/>.
        /// <para>
        ///   If this commit is the current tip of the branch as it exists in the repository, the HEAD
        ///   will point to this branch. Otherwise, the HEAD will be detached, pointing at the commit sha.
        /// </para>
        /// </summary>
        /// <param name="branch">The <see cref="Branch"/> to check out.</param>
        /// <param name="options"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        Branch Checkout(Branch branch, CheckoutOptions options);

        /// <summary>
        /// Checkout the specified branch, reference or SHA.
        /// <para>
        ///   If the committishOrBranchSpec parameter resolves to a branch name, then the checked out HEAD will
        ///   will point to the branch. Otherwise, the HEAD will be detached, pointing at the commit sha.
        /// </para>
        /// </summary>
        /// <param name="committishOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <param name="options"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        Branch Checkout(string committishOrBranchSpec, CheckoutOptions options);

        /// <summary>
        /// Checkout the specified <see cref="LibGit2Sharp.Commit"/>.
        /// <para>
        ///   Will detach the HEAD and make it point to this commit sha.
        /// </para>
        /// </summary>
        /// <param name="commit">The <see cref="LibGit2Sharp.Commit"/> to check out.</param>
        /// <param name="options"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        Branch Checkout(Commit commit, CheckoutOptions options);

        /// <summary>
        /// Updates specifed paths in the index and working directory with the versions from the specified branch, reference, or SHA.
        /// <para>
        /// This method does not switch branches or update the current repository HEAD.
        /// </para>
        /// </summary>
        /// <param name = "committishOrBranchSpec">A revparse spec for the commit or branch to checkout paths from.</param>
        /// <param name="paths">The paths to checkout.</param>
        /// <param name="checkoutOptions">Collection of parameters controlling checkout behavior.</param>
        void CheckoutPaths(string committishOrBranchSpec, IEnumerable<string> paths, CheckoutOptions checkoutOptions);

        /// <summary>
        /// Try to lookup an object by its <see cref="ObjectId"/>. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name="id">The id to lookup.</param>
        /// <returns>The <see cref="GitObject"/> or null if it was not found.</returns>
        GitObject Lookup(ObjectId id);

        /// <summary>
        /// Try to lookup an object by its sha or a reference canonical name. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name="objectish">A revparse spec for the object to lookup.</param>
        /// <returns>The <see cref="GitObject"/> or null if it was not found.</returns>
        GitObject Lookup(string objectish);

        /// <summary>
        /// Try to lookup an object by its <see cref="ObjectId"/> and <see cref="ObjectType"/>. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name="id">The id to lookup.</param>
        /// <param name="type">The kind of GitObject being looked up</param>
        /// <returns>The <see cref="GitObject"/> or null if it was not found.</returns>
        GitObject Lookup(ObjectId id, ObjectType type);

        /// <summary>
        /// Try to lookup an object by its sha or a reference canonical name and <see cref="ObjectType"/>. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name="objectish">A revparse spec for the object to lookup.</param>
        /// <param name="type">The kind of <see cref="GitObject"/> being looked up</param>
        /// <returns>The <see cref="GitObject"/> or null if it was not found.</returns>
        GitObject Lookup(string objectish, ObjectType type);

        /// <summary>
        /// Stores the content of the <see cref="Repository.Index"/> as a new <see cref="LibGit2Sharp.Commit"/> into the repository.
        /// The tip of the <see cref="Repository.Head"/> will be used as the parent of this new Commit.
        /// Once the commit is created, the <see cref="Repository.Head"/> will move forward to point at it.
        /// </summary>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <param name="author">The <see cref="Signature"/> of who made the change.</param>
        /// <param name="committer">The <see cref="Signature"/> of who added the change to the repository.</param>
        /// <param name="options">The <see cref="CommitOptions"/> that specify the commit behavior.</param>
        /// <returns>The generated <see cref="LibGit2Sharp.Commit"/>.</returns>
        Commit Commit(string message, Signature author, Signature committer, CommitOptions options);

        /// <summary>
        /// Sets the current <see cref="Head"/> to the specified commit and optionally resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="resetMode">Flavor of reset operation to perform.</param>
        /// <param name="commit">The target commit object.</param>
        void Reset(ResetMode resetMode, Commit commit);

        /// <summary>
        /// Replaces entries in the <see cref="Repository.Index"/> with entries from the specified commit.
        /// </summary>
        /// <param name="commit">The target commit object.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be considered.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        [Obsolete("This method will be removed in the next release. Please use Index.Replace() instead.")]
        void Reset(Commit commit, IEnumerable<string> paths, ExplicitPathsOptions explicitPathsOptions);

        /// <summary>
        /// Clean the working tree by removing files that are not under version control.
        /// </summary>
        void RemoveUntrackedFiles();

        /// <summary>
        /// Revert the specified commit.
        /// </summary>
        /// <param name="commit">The <see cref="Commit"/> to revert.</param>
        /// <param name="reverter">The <see cref="Signature"/> of who is performing the reverte.</param>
        /// <param name="options"><see cref="RevertOptions"/> controlling revert behavior.</param>
        /// <returns>The result of the revert.</returns>
        RevertResult Revert(Commit commit, Signature reverter, RevertOptions options);

        /// <summary>
        /// Merge changes from commit into the branch pointed at by HEAD..
        /// </summary>
        /// <param name="commit">The commit to merge into the branch pointed at by HEAD.</param>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <param name="options">Specifies optional parameters controlling merge behavior; if null, the defaults are used.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        MergeResult Merge(Commit commit, Signature merger, MergeOptions options);

        /// <summary>
        /// Merges changes from branch into the branch pointed at by HEAD..
        /// </summary>
        /// <param name="branch">The branch to merge into the branch pointed at by HEAD.</param>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <param name="options">Specifies optional parameters controlling merge behavior; if null, the defaults are used.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        MergeResult Merge(Branch branch, Signature merger, MergeOptions options);

        /// <summary>
        /// Merges changes from the commit into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="committish">The commit to merge into branch pointed at by HEAD.</param>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <param name="options">Specifies optional parameters controlling merge behavior; if null, the defaults are used.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        MergeResult Merge(string committish, Signature merger, MergeOptions options);

        /// <summary>
        /// Access to Rebase functionality.
        /// </summary>
        Rebase Rebase { get; }

        /// <summary>
        /// Merge the reference that was recently fetched. This will merge
        /// the branch on the fetched remote that corresponded to the
        /// current local branch when we did the fetch. This is the
        /// second step in performing a pull operation (after having
        /// performed said fetch).
        /// </summary>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <param name="options">Specifies optional parameters controlling merge behavior; if null, the defaults are used.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        MergeResult MergeFetchedRefs(Signature merger, MergeOptions options);

        /// <summary>
        /// Cherry picks changes from the commit into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="commit">The commit to cherry pick into branch pointed at by HEAD.</param>
        /// <param name="committer">The <see cref="Signature"/> of who is performing the cherry pick.</param>
        /// <param name="options">Specifies optional parameters controlling cherry pick behavior; if null, the defaults are used.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        CherryPickResult CherryPick(Commit commit, Signature committer, CherryPickOptions options);

        /// <summary>
        /// Manipulate the currently ignored files.
        /// </summary>
        Ignore Ignore { get; }

        /// <summary>
        /// Provides access to network functionality for a repository.
        /// </summary>
        Network Network { get; }

        ///<summary>
        /// Lookup and enumerate stashes in the repository.
        ///</summary>
        StashCollection Stashes { get; }

        /// <summary>
        /// Find where each line of a file originated.
        /// </summary>
        /// <param name="path">Path of the file to blame.</param>
        /// <param name="options">Specifies optional parameters; if null, the defaults are used.</param>
        /// <returns>The blame for the file.</returns>
        BlameHunkCollection Blame(string path, BlameOptions options);

        /// <summary>
        /// Promotes to the staging area the latest modifications of a file in the working directory (addition, updation or removal).
        ///
        /// If this path is ignored by configuration then it will not be staged unless <see cref="StageOptions.IncludeIgnored"/> is unset.
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="stageOptions">Determines how paths will be staged.</param>
        void Stage(string path, StageOptions stageOptions);

        /// <summary>
        /// Promotes to the staging area the latest modifications of a collection of files in the working directory (addition, updation or removal).
        ///
        /// Any paths (even those listed explicitly) that are ignored by configuration will not be staged unless <see cref="StageOptions.IncludeIgnored"/> is unset.
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="stageOptions">Determines how paths will be staged.</param>
        void Stage(IEnumerable<string> paths, StageOptions stageOptions);

        /// <summary>
        /// Removes from the staging area all the modifications of a file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="explicitPathsOptions">
        /// The passed <paramref name="path"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        void Unstage(string path, ExplicitPathsOptions explicitPathsOptions);

        /// <summary>
        /// Removes from the staging area all the modifications of a collection of file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="explicitPathsOptions">
        /// The passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        void Unstage(IEnumerable<string> paths, ExplicitPathsOptions explicitPathsOptions);

        /// <summary>
        /// Moves and/or renames a file in the working directory and promotes the change to the staging area.
        /// </summary>
        /// <param name="sourcePath">The path of the file within the working directory which has to be moved/renamed.</param>
        /// <param name="destinationPath">The target path of the file within the working directory.</param>
        void Move(string sourcePath, string destinationPath);

        /// <summary>
        /// Moves and/or renames a collection of files in the working directory and promotes the changes to the staging area.
        /// </summary>
        /// <param name="sourcePaths">The paths of the files within the working directory which have to be moved/renamed.</param>
        /// <param name="destinationPaths">The target paths of the files within the working directory.</param>
        void Move(IEnumerable<string> sourcePaths, IEnumerable<string> destinationPaths);

        /// <summary>
        /// Removes a file from the staging area, and optionally removes it from the working directory as well.
        /// <para>
        ///   If the file has already been deleted from the working directory, this method will only deal
        ///   with promoting the removal to the staging area.
        /// </para>
        /// <para>
        ///   The default behavior is to remove the file from the working directory as well.
        /// </para>
        /// <para>
        ///   When not passing a <paramref name="explicitPathsOptions"/>, the passed path will be treated as
        ///   a pathspec. You can for example use it to pass the relative path to a folder inside the working directory,
        ///   so that all files beneath this folders, and the folder itself, will be removed.
        /// </para>
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the file from the working directory, False otherwise.</param>
        /// <param name="explicitPathsOptions">
        /// The passed <paramref name="path"/> will be treated as an explicit path.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        void Remove(string path, bool removeFromWorkingDirectory, ExplicitPathsOptions explicitPathsOptions);

        /// <summary>
        /// Removes a collection of fileS from the staging, and optionally removes them from the working directory as well.
        /// <para>
        ///   If a file has already been deleted from the working directory, this method will only deal
        ///   with promoting the removal to the staging area.
        /// </para>
        /// <para>
        ///   The default behavior is to remove the files from the working directory as well.
        /// </para>
        /// <para>
        ///   When not passing a <paramref name="explicitPathsOptions"/>, the passed paths will be treated as
        ///   a pathspec. You can for example use it to pass the relative paths to folders inside the working directory,
        ///   so that all files beneath these folders, and the folders themselves, will be removed.
        /// </para>
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the files from the working directory, False otherwise.</param>
        /// <param name="explicitPathsOptions">
        /// The passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        void Remove(IEnumerable<string> paths, bool removeFromWorkingDirectory, ExplicitPathsOptions explicitPathsOptions);

        /// <summary>
        /// Retrieves the state of a file in the working directory, comparing it against the staging area and the latest commit.
        /// </summary>
        /// <param name="filePath">The relative path within the working directory to the file.</param>
        /// <returns>A <see cref="FileStatus"/> representing the state of the <paramref name="filePath"/> parameter.</returns>
        FileStatus RetrieveStatus(string filePath);

        /// <summary>
        /// Retrieves the state of all files in the working directory, comparing them against the staging area and the latest commit.
        /// </summary>
        /// <param name="options">If set, the options that control the status investigation.</param>
        /// <returns>A <see cref="RepositoryStatus"/> holding the state of all the files.</returns>
        RepositoryStatus RetrieveStatus(StatusOptions options);

        /// <summary>
        /// Finds the most recent annotated tag that is reachable from a commit.
        /// <para>
        ///   If the tag points to the commit, then only the tag is shown. Otherwise,
        ///   it suffixes the tag name with the number of additional commits on top
        ///   of the tagged object and the abbreviated object name of the most recent commit.
        /// </para>
        /// <para>
        ///   Optionally, the <paramref name="options"/> parameter allow to tweak the
        ///   search strategy (considering lightweith tags, or even branches as reference points)
        ///   and the formatting of the returned identifier.
        /// </para>
        /// </summary>
        /// <param name="commit">The commit to be described.</param>
        /// <param name="options">Determines how the commit will be described.</param>
        /// <returns>A descriptive identifier for the commit based on the nearest annotated tag.</returns>
        string Describe(Commit commit, DescribeOptions options);
    }
}
