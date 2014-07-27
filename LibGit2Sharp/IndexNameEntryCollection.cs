using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// The collection of <see cref="LibGit2Sharp.IndexNameEntry"/>s in a
    /// <see cref="LibGit2Sharp.Repository"/> index that reflect the
    /// original paths of any rename conflicts that exist in the index.
    /// </summary>
    public class IndexNameEntryCollection : IEnumerable<IndexNameEntry>
    {
        private readonly Repository repo;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected IndexNameEntryCollection()
        { }

        internal IndexNameEntryCollection(Repository repo)
        {
            this.repo = repo;
        }

        private IndexNameEntry this[int index]
        {
            get
            {
                IndexNameEntrySafeHandle entryHandle = Proxy.git_index_name_get_byindex(repo.Index.Handle, (UIntPtr)index);
                return IndexNameEntry.BuildFromPtr(entryHandle);
            }
        }

        #region IEnumerable<IndexNameEntry> Members

        private List<IndexNameEntry> AllIndexNames()
        {
            var list = new List<IndexNameEntry>();

            int count = Proxy.git_index_name_entrycount(repo.Index.Handle);

            for (int i = 0; i < count; i++)
            {
                list.Add(this[i]);
            }

            return list;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<IndexNameEntry> GetEnumerator()
        {
            return AllIndexNames().GetEnumerator();
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
    }
}
