using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The collection of <see cref = "Stash" />es in a <see cref = "Repository" />
    /// </summary>
    public class StashCollection
    {
        internal readonly Repository repo;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected StashCollection()
        { }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "StashCollection" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        internal StashCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Creates a stash with the specified message.
        /// </summary>
        /// <param name="stasher">The <see cref="Signature"/> of the user who stashes </param>
        /// <param name = "message">The message of the stash.</param>
        /// <param name = "options">A combination of <see cref="StashOptions"/> flags</param>
        /// <returns>the newly created <see cref="Stash"/></returns>
        public virtual Stash Add(Signature stasher, string message = null, StashOptions options = StashOptions.Default)
        {
            Ensure.ArgumentNotNull(stasher, "stasher");

            string prettifiedMessage = Proxy.git_message_prettify(string.IsNullOrEmpty(message) ? string.Empty : message);

            ObjectId oid = Proxy.git_stash_save(repo.Handle, stasher, prettifiedMessage, options);

            // in case there is nothing to stash
            if (oid == null)
            {
                return null;
            }

            return new Stash(repo, oid);
        }
    }
}
