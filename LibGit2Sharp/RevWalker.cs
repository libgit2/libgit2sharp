using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Creates a new revision walker to iterate through repository.
    /// </summary>
    /// <remarks>
    /// This revision walker uses a custom memory pool and an internal commit cache, so it is relatively expensive to allocate.
    ///
    /// For maximum performance, this revision walker should be reused for different walks.
    ///
    /// This revision walker is *not* thread safe: it may only be used to walk a repository on a single thread;
    /// however, it is possible to have several revision walkers in several different threads walking the same repository.
    /// </remarks>
    public sealed class RevWalker : IDisposable
    {
        private readonly RevWalkerHandle handle;

        /// <summary>
        /// Creates a new revision walker to iterate through a repo.
        /// </summary>
        public RevWalker(Repository repo)
        {
            handle = Proxy.git_revwalk_new(repo.Handle);
            repo.RegisterForCleanup(handle);
        }

        /// <summary>
        /// Resets the revision walker for reuse.
        /// </summary>
        /// <remarks>
        /// This will clear all the pushed and hidden commits, and leave the walker in a blank state (just like at creation)
        /// ready to receive new commit pushes and start a new walk.
        ///
        /// The revision walk is automatically reset when a walk is over.
        /// </remarks>
        public void Reset()
        {
            Proxy.git_revwalk_reset(handle);
        }

        /// <summary>
        /// Change the sorting mode when iterating through the repository's contents.
        /// </summary>
        /// <remarks>
        /// Changing the sorting mode resets the walker.
        /// </remarks>
        public void Sorting(CommitSortStrategies sortMode)
        {
            Proxy.git_revwalk_sorting(handle, sortMode);
        }

        /// <summary>
        /// Gets the next commit from the revision walk.
        /// </summary>
        /// <remarks>
        /// The initial call to this method is *not* blocking when iterating through a repo with a time-sorting mode.
        ///
        /// Iterating with Topological or inverted modes makes the initial call blocking to preprocess the commit list,
        /// but this block should be mostly unnoticeable on most repositories (topological preprocessing times at 0.3s
        /// on the git.git repo).
        ///
        /// The revision walker is reset when the walk is over.
        ///</remarks>
        /// <returns>New commit ID or null on error.</returns>
        public ObjectId Next()
        {
            return Proxy.git_revwalk_next(handle);
        }

        /// <summary>
        /// Marks a commit (and its ancestors) uninteresting for the output.
        /// </summary>
        /// <remarks>
        /// The given ID must belong to a committish on the walked repository.
        ///
        /// The resolved commit and all its parents will be hidden from the output on the revision walk.
        /// </remarks>
        public void Hide(ObjectId commitId)
        {
            Proxy.git_revwalk_hide(handle, commitId);
        }

        /// <summary>
        /// Hide matching references.
        /// </summary>
        /// <remarks>
        /// The OIDs pointed to by the references that match the given glob pattern and their ancestors will be hidden
        /// from the output on the// revision walk.
        ///
        /// A leading 'refs/' is implied if not present as well as a trailing '/\*' if the glob lacks '?', '\*' or '['.
        ///
        /// Any references matching this glob which do not point to a committish will be ignored.
        /// </remarks>
        /// <param name="glob">the glob pattern references should match</param>
        public void HideGlob(string glob)
        {
            Proxy.git_revwalk_hide_glob(handle, glob);
        }

        /// <summary>
        /// Hide the OID pointed to by a reference
        /// </summary>
        /// <remarks>
        /// The reference must point to a committish.
        /// </remarks>
        /// <param name="refName">the reference to hide</param>
        public void HideRef(string refName)
        {
            Proxy.git_revwalk_hide_ref(handle, refName);
        }

        /// <summary>
        /// Marks a commit to start traversal from.
        /// </summary>
        /// <remarks>
        /// The given OID must belong to a commit on the walked repository.
        ///
        /// The given commit will be used as one of the roots when starting the revision walk. At least one commit must
        /// be pushed the repository before a walk can be started.</remarks>
        public void Push(ObjectId commitId)
        {
            Proxy.git_revwalk_push(handle, commitId);
        }

        /// <summary>
        /// Push matching references
        /// </summary>
        /// <remarks>
        /// The OIDs pointed to by the references that match the given glob pattern will be pushed to the revision walker.
        ///
        /// A leading 'refs/' is implied if not present as well as a trailing '/\*' if the glob lacks '?', '\*' or '['.
        ///
        ///  Any references matching this glob which do not point to a committish will be ignored.
        /// </remarks>
        /// <param name="glob">The glob pattern references should match</param>
        public void PushGlob(string glob)
        {
            Proxy.git_revwalk_push_glob(handle, glob);
        }

        /// <summary>
        /// Push the OID pointed to by a reference
        /// </summary>
        /// <remarks>
        ///  The reference must point to a committish.
        /// </remarks>
        public void PushRef(string refName)
        {
            Proxy.git_revwalk_push_ref(handle, refName);
        }

        /// <summary>
        /// Simplify the history by first-parent.
        /// </summary>
        /// <remarks>
        /// No parents other than the first for each commit will be enqueued.
        /// </remarks>
        public void SimplifyFirstParent()
        {
            Proxy.git_revwalk_simplify_first_parent(handle);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            handle.SafeDispose();
        }
    }
}
