using System;
using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// The collection of <see cref="LibGit2Sharp.IndexReucEntry"/>s in a
    /// <see cref="LibGit2Sharp.Repository"/> index that reflect the
    /// resolved conflicts.
    /// </summary>
    public class IndexReucEntryCollection : IEnumerable<IndexReucEntry>
    {
        private readonly Index index;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected IndexReucEntryCollection()
        { }

        internal IndexReucEntryCollection(Index index)
        {
            this.index = index;
        }

        /// <summary>
        /// Gets the <see cref="IndexReucEntry"/> with the specified relative path.
        /// </summary>
        public virtual unsafe IndexReucEntry this[string path]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(path, "path");

                git_index_reuc_entry* entryHandle = Proxy.git_index_reuc_get_bypath(index.Handle, path);
                return IndexReucEntry.BuildFromPtr(entryHandle);
            }
        }

        private unsafe IndexReucEntry this[int idx]
        {
            get
            {
                git_index_reuc_entry* entryHandle = Proxy.git_index_reuc_get_byindex(index.Handle, (UIntPtr)idx);
                return IndexReucEntry.BuildFromPtr(entryHandle);
            }
        }

        #region IEnumerable<IndexReucEntry> Members

        private List<IndexReucEntry> AllIndexReucs()
        {
            var list = new List<IndexReucEntry>();

            int count = Proxy.git_index_reuc_entrycount(index.Handle);

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
        public virtual IEnumerator<IndexReucEntry> GetEnumerator()
        {
            return AllIndexReucs().GetEnumerator();
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
