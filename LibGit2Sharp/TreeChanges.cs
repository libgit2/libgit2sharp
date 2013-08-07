using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Holds the result of a diff between two trees.
    /// <para>Changes at the granularity of the file can be obtained through the different sub-collections <see cref="Added"/>, <see cref="Deleted"/> and <see cref="Modified"/>.</para>
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
        private int linesAdded;
        private int linesDeleted;

        private readonly IDictionary<ChangeKind, Action<TreeChanges, TreeEntryChanges>> fileDispatcher = Build();

        private readonly StringBuilder fullPatchBuilder = new StringBuilder();

        private static IDictionary<ChangeKind, Action<TreeChanges, TreeEntryChanges>> Build()
        {
            return new Dictionary<ChangeKind, Action<TreeChanges, TreeEntryChanges>>
                       {
                           { ChangeKind.Modified, (de, d) => de.modified.Add(d) },
                           { ChangeKind.Deleted, (de, d) => de.deleted.Add(d) },
                           { ChangeKind.Added, (de, d) => de.added.Add(d) },
                           { ChangeKind.TypeChanged, (de, d) => de.typeChanged.Add(d) },
                           { ChangeKind.Unmodified, (de, d) => de.unmodified.Add(d) },
                       };
        }

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected TreeChanges()
        { }

        internal TreeChanges(DiffListSafeHandle diff, bool skipPatchBuilding = false)
        {
            Proxy.git_diff_foreach(diff, FileCallback, null, DataCallback);

            if (!skipPatchBuilding)
            {
                Proxy.git_diff_print_patch(diff, PrintCallBack);
            }
        }

        private int DataCallback(GitDiffDelta delta, GitDiffRange range, GitDiffLineOrigin lineOrigin, IntPtr content, UIntPtr contentLen, IntPtr payload)
        {
            var filePath = FilePathMarshaler.FromNative(delta.NewFile.Path);

            AddLineChange(this[filePath], lineOrigin);

            return 0;
        }

        private int FileCallback(GitDiffDelta delta, float progress, IntPtr payload)
        {
            AddFileChange(delta);
            return 0;
        }

        private int PrintCallBack(GitDiffDelta delta, GitDiffRange range, GitDiffLineOrigin lineorigin, IntPtr content, UIntPtr contentlen, IntPtr payload)
        {
            string formattedoutput = Utf8Marshaler.FromNative(content, (int)contentlen);
            var filePath = FilePathMarshaler.FromNative(delta.NewFile.Path);

            fullPatchBuilder.Append(formattedoutput);
            this[filePath].AppendToPatch(formattedoutput);

            return 0;
        }

        private void AddLineChange(Changes currentChange, GitDiffLineOrigin lineOrigin)
        {
            switch (lineOrigin)
            {
                case GitDiffLineOrigin.GIT_DIFF_LINE_ADDITION:
                    linesAdded++;
                    currentChange.LinesAdded++;
                    break;

                case GitDiffLineOrigin.GIT_DIFF_LINE_DELETION:
                    linesDeleted++;
                    currentChange.LinesDeleted++;
                    break;
            }
        }

        private void AddFileChange(GitDiffDelta delta)
        {
            var newFilePath = FilePathMarshaler.FromNative(delta.NewFile.Path);

            var oldFilePath = FilePathMarshaler.FromNative(delta.OldFile.Path);
            var newMode = (Mode)delta.NewFile.Mode;
            var oldMode = (Mode)delta.OldFile.Mode;
            var newOid = delta.NewFile.Oid;
            var oldOid = delta.OldFile.Oid;

            if (delta.Status == ChangeKind.Untracked || delta.Status == ChangeKind.Ignored)
            {
                delta.Status = ChangeKind.Added;
            }

            var diffFile = new TreeEntryChanges(newFilePath, newMode, newOid, delta.Status, oldFilePath, oldMode, oldOid, delta.IsBinary());

            fileDispatcher[delta.Status](this, diffFile);
            changes.Add(newFilePath, diffFile);
        }

        #region IEnumerable<Tag> Members

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
        /// The total number of lines added in this diff.
        /// </summary>
        public virtual int LinesAdded
        {
            get { return linesAdded; }
        }

        /// <summary>
        /// The total number of lines added in this diff.
        /// </summary>
        public virtual int LinesDeleted
        {
            get { return linesDeleted; }
        }

        /// <summary>
        /// The full patch file of this diff.
        /// </summary>
        public virtual string Patch
        {
            get { return fullPatchBuilder.ToString(); }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "+{0} ~{2} -{1} \u00B1{3}",
                    Added.Count(), Deleted.Count(),
                    Modified.Count(), TypeChanged.Count());
            }
        }
    }
}
