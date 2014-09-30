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

        /// <summary>
        /// A GitLink cannot be dereferenced to a commit - throws or returns null.
        /// </summary>
        /// <param name="throwsIfCanNotBeDereferencedToACommit"></param>
        /// <returns></returns>
        internal override Commit DereferenceToCommit(bool throwsIfCanNotBeDereferencedToACommit)
        {
            if (throwsIfCanNotBeDereferencedToACommit)
            {
                throw new CannotDereferenceException("Cannot dereference a git-link object to a commit.");
            }

            return null;
        }

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
