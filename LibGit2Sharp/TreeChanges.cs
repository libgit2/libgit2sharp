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
    public class TreeChanges : IEnumerable<TreeEntryChanges>
    {
        private readonly IDictionary<FilePath, TreeEntryChanges> changes = new Dictionary<FilePath, TreeEntryChanges>();
        private readonly List<TreeEntryChanges> added = new List<TreeEntryChanges>();
        private readonly List<TreeEntryChanges> deleted = new List<TreeEntryChanges>();
        private readonly List<TreeEntryChanges> modified = new List<TreeEntryChanges>();
        private readonly List<TreeEntryChanges> typeChanged = new List<TreeEntryChanges>();
        private readonly List<TreeEntryChanges> unmodified = new List<TreeEntryChanges>();
        private readonly List<TreeEntryChanges> renamed = new List<TreeEntryChanges>();
        private readonly List<TreeEntryChanges> copied = new List<TreeEntryChanges>();

        private readonly IDictionary<ChangeKind, Action<TreeChanges, TreeEntryChanges>> fileDispatcher = Build();

        private static IDictionary<ChangeKind, Action<TreeChanges, TreeEntryChanges>> Build()
        {
            return new Dictionary<ChangeKind, Action<TreeChanges, TreeEntryChanges>>
                       {
                           { ChangeKind.Modified,    (de, d) => de.modified.Add(d) },
                           { ChangeKind.Deleted,     (de, d) => de.deleted.Add(d) },
                           { ChangeKind.Added,       (de, d) => de.added.Add(d) },
                           { ChangeKind.TypeChanged, (de, d) => de.typeChanged.Add(d) },
                           { ChangeKind.Unmodified,  (de, d) => de.unmodified.Add(d) },
                           { ChangeKind.Renamed,     (de, d) => de.renamed.Add(d) },
                           { ChangeKind.Copied,      (de, d) => de.copied.Add(d) },
                       };
        }

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected TreeChanges()
        { }

        internal TreeChanges(DiffSafeHandle diff)
        {
            Proxy.git_diff_foreach(diff, FileCallback, null, null);
        }

        private int FileCallback(GitDiffDelta delta, float progress, IntPtr payload)
        {
            AddFileChange(delta);
            return 0;
        }

        private void AddFileChange(GitDiffDelta delta)
        {
            var treeEntryChanges = new TreeEntryChanges(delta);

            fileDispatcher[treeEntryChanges.Status](this, treeEntryChanges);
            changes.Add(treeEntryChanges.Path, treeEntryChanges);
        }

        #region IEnumerable<TreeEntryChanges> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<TreeEntryChanges> GetEnumerator()
        {
            return changes.Values.GetEnumerator();
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
        /// Gets the <see cref="TreeEntryChanges"/> corresponding to the specified <paramref name="path"/>.
        /// </summary>
        public virtual TreeEntryChanges this[string path]
        {
            get { return this[(FilePath)path]; }
        }

        private TreeEntryChanges this[FilePath path]
        {
            get
            {
                TreeEntryChanges treeEntryChanges;
                if (changes.TryGetValue(path, out treeEntryChanges))
                {
                    return treeEntryChanges;
                }

                return null;
            }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> that have been been added.
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Added
        {
            get { return added; }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> that have been deleted.
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Deleted
        {
            get { return deleted; }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> that have been modified.
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Modified
        {
            get { return modified; }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which type have been changed.
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> TypeChanged
        {
            get { return typeChanged; }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which have been renamed
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Renamed
        {
            get { return renamed; }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which have been copied
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Copied
        {
            get { return copied; }
        }

        /// <summary>
        /// List of <see cref="TreeEntryChanges"/> which are unmodified
        /// </summary>
        public virtual IEnumerable<TreeEntryChanges> Unmodified
        {
            get { return unmodified; }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "+{0} ~{1} -{2} \u00B1{3} R{4} C{5}",
                    Added.Count(), Modified.Count(), Deleted.Count(),
                    TypeChanged.Count(), Renamed.Count(), Copied.Count());
            }
        }
    }
}
