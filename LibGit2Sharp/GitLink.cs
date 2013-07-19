using System.Diagnostics;

namespace LibGit2Sharp
{
    /// <summary>
    /// Represents a gitlink (a reference to a commit in another Git repository)
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class GitLink : GitObject
    {
        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected GitLink()
        { }

        internal GitLink(Repository repo, ObjectId id)
            : base(repo, id)
        {
        }

        private string DebuggerDisplay
        {
            get { return Id.ToString(); }
        }
    }
}
