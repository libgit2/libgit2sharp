using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// The collection of <see cref="Stash"/>es in a <see cref="Repository"/>
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class StashCollection : IEnumerable<Stash>
    {
        internal readonly Repository repo;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected StashCollection()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StashCollection"/> class.
        /// </summary>
        /// <param name="repo">The repo.</param>
        internal StashCollection(Repository repo)
        {
            this.repo = repo;
        }

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// <para>
        ///   The enumerator returns the stashes by descending order (last stash is returned first).
        /// </para>
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Stash> GetEnumerator()
        {
            return Proxy.git_stash_foreach(repo.Handle,
                (index, message, commitId) => new Stash(repo, new ObjectId(commitId), index)).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Gets the <see cref="Stash"/> corresponding to the specified index (0 being the most recent one).
        /// </summary>
        public virtual Stash this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index", "The passed index must be a positive integer.");
                }

                GitObject stashCommit = repo.Lookup(string.Format("stash@{{{0}}}", index), GitObjectType.Commit, LookUpOptions.None);

                return stashCommit == null ? null : new Stash(repo, stashCommit.Id, index);
            }
        }

        /// <summary>
        /// Creates a stash with the specified message.
        /// </summary>
        /// <param name="stasher">The <see cref="Signature"/> of the user who stashes </param>
        /// <param name="message">The message of the stash.</param>
        /// <param name="options">A combination of <see cref="StashModifiers"/> flags</param>
        /// <returns>the newly created <see cref="Stash"/></returns>
        public virtual Stash Add(Signature stasher, string message = null, StashModifiers options = StashModifiers.Default)
        {
            Ensure.ArgumentNotNull(stasher, "stasher");

            string prettifiedMessage = Proxy.git_message_prettify(string.IsNullOrEmpty(message) ? string.Empty : message);

            ObjectId oid = Proxy.git_stash_save(repo.Handle, stasher, prettifiedMessage, options);

            // in case there is nothing to stash
            if (oid == null)
            {
                return null;
            }

            return new Stash(repo, oid, 0);
        }

        /// <summary>
        /// Remove a single stashed state from the stash list.
        /// </summary>
        /// <param name="index">The index of the stash to remove (0 being the most recent one).</param>
        public virtual void Remove(int index)
        {
            if (index < 0)
            {
                throw new ArgumentException("The passed index must be a positive integer.", "index");
            }

            Proxy.git_stash_drop(repo.Handle, index);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "Count = {0}", this.Count());
            }
        }
    }
}
