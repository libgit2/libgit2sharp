﻿using System;
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
            throw new NotImplementedException();
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
            if(oid == null)
            {
                return null;
            }

            return new Stash(repo, oid, 0);
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
