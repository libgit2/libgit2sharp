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

            if (!Reference.IsValidName(canonicalName))
            {
                throw new InvalidSpecificationException(
                    string.Format(CultureInfo.InvariantCulture, "The given reference name '{0}' is not valid", canonicalName));
            }

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
        public virtual IEnumerator<ReflogEntry> GetEnumerator()
        {
            var entries = new List<ReflogEntry>();

            using (ReflogSafeHandle reflog = Proxy.git_reflog_read(repo.Handle, canonicalName))
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
                return string.Format(CultureInfo.InvariantCulture, "Count = {0}", this.Count());
            }
        }
    }
}
