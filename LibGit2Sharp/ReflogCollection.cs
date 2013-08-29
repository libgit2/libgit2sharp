using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// The <see cref="ReflogCollection"/> is the reflog of a given <see cref="Reference"/>, as a enumerable of <see cref="ReflogEntry"/>.
    /// Reflog is a mechanism to record when the tip of a <see cref="Branch"/> is updated.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ReflogCollection : IEnumerable<ReflogEntry>
    {
        internal readonly Repository repo;

        private readonly string canonicalName;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected ReflogCollection()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflogCollection"/> class.
        /// </summary>
        /// <param name="repo">The repo.</param>
        /// <param name="canonicalName">the canonical name of the <see cref="Reference"/> to retrieve reflog entries on.</param>
        internal ReflogCollection(Repository repo, string canonicalName)
        {
            Ensure.ArgumentNotNullOrEmptyString(canonicalName, "canonicalName");
            Ensure.ArgumentNotNull(repo, "repo");

            this.repo = repo;
            this.canonicalName = canonicalName;
        }

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// <para>
        ///   The enumerator returns the <see cref="ReflogEntry"/> by descending order (last reflog entry is returned first).
        /// </para>
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public IEnumerator<ReflogEntry> GetEnumerator()
        {
            var entries = new List<ReflogEntry>();

            using (ReferenceSafeHandle reference = Proxy.git_reference_lookup(repo.Handle, canonicalName, true))
            using (ReflogSafeHandle reflog = Proxy.git_reflog_read(reference))
            {
                var entriesCount = Proxy.git_reflog_entrycount(reflog);

                for (int i = 0; i < entriesCount; i++)
                {
                    ReflogEntrySafeHandle handle = Proxy.git_reflog_entry_byindex(reflog, i);
                    entries.Add(new ReflogEntry(handle));
                }
            }

            return entries.GetEnumerator();
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

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "Count = {0}", this.Count());
            }
        }

        /// <summary>
        /// Add a new <see cref="ReflogEntry"/> to the current <see cref="ReflogCollection"/>. It will be created as first item of the collection
        /// The native reflog object will be saved right after inserting the entry.
        /// </summary>
        /// <param name="target">the <see cref="ObjectId"/> of the new target the <see cref="Reference"/> will point out to.</param>
        /// <param name="reflogMessage">the message associated with the new <see cref="ReflogEntry"/>.</param>
        /// <param name="committer"><see cref="Signature"/> of the comitter.</param>
        internal virtual void Append(ObjectId target, string reflogMessage, Signature committer)
        {
            var logAllRefUpdates = repo.Config.GetValueOrDefault<bool>("core.logAllRefUpdates", false);
            if (!logAllRefUpdates)
            {
                return;
            }

            using (ReferenceSafeHandle reference = Proxy.git_reference_lookup(repo.Handle, canonicalName, true))
            using (ReflogSafeHandle reflog = Proxy.git_reflog_read(reference))
            {
                string prettifiedMessage = Proxy.git_message_prettify(reflogMessage);
                Proxy.git_reflog_append(reflog, target, committer, prettifiedMessage);
            }
        }

        /// <summary>
        /// Add a new <see cref="ReflogEntry"/> to the current <see cref="ReflogCollection"/>. It will be created as first item of the collection
        /// The native reflog object will be saved right after inserting the entry.
        /// <para>
        ///   The <see cref="Signature"/> will be built from the current Git configuration.
        /// </para>
        /// </summary>
        /// <param name="target">the <see cref="ObjectId"/> of the new target the <see cref="Reference"/> will point out to.</param>
        /// <param name="reflogMessage">the message associated with the new <see cref="ReflogEntry"/>.</param>
        internal void Append(ObjectId target, string reflogMessage)
        {
            Signature author = repo.Config.BuildSignatureFromGlobalConfiguration(DateTimeOffset.Now, false);
            Append(target, reflogMessage, author);
        }
    }
}
