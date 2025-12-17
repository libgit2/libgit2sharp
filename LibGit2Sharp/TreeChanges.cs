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
    /// Holds the result of a diff between two trees.
    /// <para>Changes at the granularity of the file can be obtained through the different sub-collections <see cref="Added"/>, <see cref="Deleted"/> and <see cref="Modified"/>.</para>
    /// <para>To obtain the actual patch of the diff, use the <see cref="Patch"/> class when calling Compare.</para>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TreeChanges : IEnumerable<TreeEntryChanges>, IDiffResult
    {
        private readonly DiffHandle diff;
        private readonly Lazy<int> count;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected TreeChanges()
        { }

        internal unsafe TreeChanges(DiffHandle diff)
        {
            this.diff = diff;
            this.count = new Lazy<int>(() => Proxy.git_diff_num_deltas(diff));
        }

        /// <summary>
        /// Enumerates the diff and yields deltas with the specified change kind.
        /// </summary>
        /// <param name="changeKind">Change type to filter on.</param>
        private IEnumerable<TreeEntryChanges> GetChangesOfKind(ChangeKind changeKind)
        {
            TreeEntryChanges entry;
            for (int i = 0; i < Count; i++)
            {
                if (TryGetEntryWithChangeTypeAt(i, changeKind, out entry))
                {
                    yield return entry;
                }
            }
        }

        /// <summary>
        /// This is method exists to work around .net not allowing unsafe code
        /// in iterators.
        /// </summary>
        private unsafe bool TryGetEntryWithChangeTypeAt(int index, ChangeKind changeKind, out TreeEntryChanges entry)
        {
            if (index < 0 || index > count.Value)
                throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the collection.");

            var delta = Proxy.git_diff_get_delta(diff, index);

            if (TreeEntryChanges.GetStatusFromChangeKind(delta->status) == changeKind)
            {
                entry = new TreeEntryChanges(delta);
                return true;
            }

            entry = null;
            return false;
        }

        #region IEnumerable<TreeEntryChanges> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<TreeEntryChanges> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return GetEntryAt(i);
            }
        }

        /// <summary>
        /// This is method exists to work around .net not allowing unsafe code
        /// in iterators.
        /// </summary>
        private unsafe TreeEntryChanges GetEntryAt(int index)
        {
            if (index < 0 || index > count.Value)
                throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the collection.");

            return new TreeEntryChanges(Proxy.git_diff_get_delta(diff, index));
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
        /// List of <see cref="TreeEntryChanges"/> that have been been added.
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Added
        {
            get { return GetChangesOfKind(ChangeKind.Added); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> that have been deleted.
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Deleted
        {
            get { return GetChangesOfKind(ChangeKind.Deleted); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> that have been modified.
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Modified
        {
            get { return GetChangesOfKind(ChangeKind.Modified); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which type have been changed.
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> TypeChanged
        {
            get { return GetChangesOfKind(ChangeKind.TypeChanged); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which have been renamed
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Renamed
        {
            get { return GetChangesOfKind(ChangeKind.Renamed); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which have been copied
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Copied
        {
            get { return GetChangesOfKind(ChangeKind.Copied); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which are unmodified
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Unmodified
        {
            get { return GetChangesOfKind(ChangeKind.Unmodified); }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which are conflicted
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Conflicted
        {
            get { return GetChangesOfKind(ChangeKind.Conflicted); }
        }

        /// <summary>
        /// Gets the number of <see cref="TreeEntryChanges"/> in this comparison.
        /// </summary>
        public virtual int Count
        {
            get { return count.Value; }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "+{0} ~{1} -{2} \u00B1{3} R{4} C{5}",
                                     Added.Count(),
                                     Modified.Count(),
                                     Deleted.Count(),
                                     TypeChanged.Count(),
                                     Renamed.Count(),
                                     Copied.Count());
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            diff.SafeDispose();
        }
    }
}
