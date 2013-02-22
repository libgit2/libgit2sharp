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
    ///   The collection of <see cref = "Stash" />es in a <see cref = "Repository" />
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class StashCollection : IEnumerable<Stash>
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

        #region Implementation of IEnumerable

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Stash> GetEnumerator()
        {
            return Proxy.git_stash_foreach(repo.Handle,
                (index, message, commitId) => new Stash(repo, new ObjectId(commitId), index)).GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

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

            return new Stash(repo, oid, 0);
        }

        /// <summary>
        ///   Remove a single stashed state from the stash list.
        /// </summary>
        /// <param name = "stashRefLog">The log reference of the stash to delete. Pattern is "stash@{i}" where i is the index of the stash to remove</param>
        public virtual void Remove(string stashRefLog)
        {
            Ensure.ArgumentNotNullOrEmptyString(stashRefLog, "stashRefLog");

            int index;
            if(!TryExtractStashIndexFromRefLog(stashRefLog, out index) || index < 0)
            {
                throw new ArgumentException("must be a valid stash log reference. Pattern is 'stash@{i}' where 'i' is an integer", "stashRefLog");
            }

            Proxy.git_stash_drop(repo.Handle, index);
        }

        private static bool TryExtractStashIndexFromRefLog(string stashRefLog, out int index)
        {
            index = -1;

            if (!stashRefLog.StartsWith("stash@{"))
            {
                return false;
            }

            if (!stashRefLog.EndsWith("}"))
            {
                return false;
            }

            var indexAsString = stashRefLog.Substring(7, stashRefLog.Length - 8);

            return int.TryParse(indexAsString, out index);
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
