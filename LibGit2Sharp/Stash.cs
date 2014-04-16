using System.Globalization;
using System.Linq;

namespace LibGit2Sharp
{
    ///<summary>
    /// A Stash
    /// <para>A stash is a snapshot of the dirty state of the working directory (i.e. the modified tracked files and staged changes)</para>
    ///</summary>
    public class Stash : ReferenceWrapper<Commit>
    {
        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Stash()
        { }

        internal Stash(Repository repo, ObjectId targetId, int index)
            : base(repo, new DirectReference(string.Format(CultureInfo.InvariantCulture, "stash@{{{0}}}", index), repo, targetId), r => r.CanonicalName)
        { }

        /// <summary>
        /// Gets the <see cref="Commit"/> that contains to the captured content of the worktree when the
        /// stash was created.
        /// </summary>
        public virtual Commit WorkTree
        {
            get { return TargetObject; }
        }

        /// <summary>
        /// Gets the base <see cref="Commit"/> (i.e. the HEAD when the stash was
        /// created).
        /// </summary>
        public virtual Commit Base
        {
            get { return TargetObject.Parents.First(); }
        }

        /// <summary>
        /// Gets the <see cref="Commit"/> that contains the captured content of the index when the stash was
        /// created.
        /// </summary>
        public virtual Commit Index
        {
            get { return GetParentAtOrDefault(1); }
        }

        /// <summary>
        /// Gets the <see cref="Commit"/> that contains the list of either the untracked files, the ignored files, or both,
        /// depending on the <see cref="StashModifiers"/> options passed when the stash was created.
        /// </summary>
        public virtual Commit Untracked
        {
            get { return GetParentAtOrDefault(2); }
        }

        private Commit GetParentAtOrDefault(int parentIndex)
        {
            return TargetObject.Parents.ElementAtOrDefault(parentIndex);
        }

        /// <summary>
        /// Gets the message associated to this <see cref="Stash"/>.
        /// </summary>
        public virtual string Message
        {
            get { return WorkTree.Message; }
        }

        /// <summary>
        /// Returns "stash@{i}", where i is the index of this <see cref="Stash"/>.
        /// </summary>
        protected override string Shorten()
        {
            return CanonicalName;
        }
    }
}
