using System.Globalization;

namespace LibGit2Sharp
{
    /// <summary>
    /// A merge head is a parent for the next commit.
    /// </summary>
    public class MergeHead : ReferenceWrapper<Commit>
    {
        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected MergeHead()
        { }

        internal MergeHead(Repository repo, ObjectId targetId, int index)
            : base(repo, new DirectReference(string.Format(CultureInfo.InvariantCulture, "MERGE_HEAD[{0}]", index), repo, targetId), r => r.CanonicalName)
        {
        }

        /// <summary>
        /// Gets the <see cref="Commit"/> that this merge head points to.
        /// </summary>
        public virtual Commit Tip
        {
            get { return TargetObject; }
        }

        /// <summary>
        /// Returns "MERGE_HEAD[i]", where i is the index of this merge head.
        /// </summary>
        protected override string Shorten()
        {
            return CanonicalName;
        }
    }
}
