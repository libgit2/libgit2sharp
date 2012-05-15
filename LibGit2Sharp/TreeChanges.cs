using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Holds the result of a diff between two trees.
    ///   <para>Changes at the granularity of the file can be obtained through the different sub-collections <see cref="Added"/>, <see cref="Deleted"/> and <see cref="Modified"/>.</para>
    /// </summary>
    public class TreeChanges : IEnumerable<TreeEntryChanges>
    {
        private static readonly Utf8Marshaler marshaler = (Utf8Marshaler)Utf8Marshaler.GetInstance(string.Empty);

        private readonly IDictionary<string, TreeEntryChanges> changes = new Dictionary<string, TreeEntryChanges>();
        private readonly List<TreeEntryChanges> added = new List<TreeEntryChanges>();
        private readonly List<TreeEntryChanges> deleted = new List<TreeEntryChanges>();
        private readonly List<TreeEntryChanges> modified = new List<TreeEntryChanges>();
        private int linesAdded;
        private int linesDeleted;

        private readonly IDictionary<ChangeKind, Action<TreeChanges, TreeEntryChanges>> fileDispatcher = Build();
        private readonly string patch;

        private static IDictionary<ChangeKind, Action<TreeChanges, TreeEntryChanges>> Build()
        {
            return new Dictionary<ChangeKind, Action<TreeChanges, TreeEntryChanges>>
                       {
                           { ChangeKind.Modified, (de, d) => de.modified.Add(d) },
                           { ChangeKind.Deleted, (de, d) => de.deleted.Add(d) },
                           { ChangeKind.Added, (de, d) => de.added.Add(d) },
                       };
        }

        internal TreeChanges(DiffListSafeHandle diff)
        {
            var fullPatchBuilder = new StringBuilder();

            Ensure.Success(NativeMethods.git_diff_foreach(diff, IntPtr.Zero, FileCallback, null, LineCallback));
            Ensure.Success(NativeMethods.git_diff_print_patch(diff, IntPtr.Zero, new PatchPrinter(changes, fullPatchBuilder).PrintCallBack));

            patch = fullPatchBuilder.ToString();
        }

        private int LineCallback(IntPtr data, GitDiffDelta delta, GitDiffRange range, GitDiffLineOrigin lineorigin, IntPtr content, IntPtr contentlen)
        {
            var newFilePath = (string)marshaler.MarshalNativeToManaged(delta.NewFile.Path);

            switch (lineorigin)
            {
                case GitDiffLineOrigin.GIT_DIFF_LINE_ADDITION:
                    IncrementLinesAdded(newFilePath);
                    break;

                case GitDiffLineOrigin.GIT_DIFF_LINE_DELETION:
                    IncrementLinesDeleted(newFilePath);
                    break;
            }

            return 0;
        }

        private void IncrementLinesDeleted(string filePath)
        {
            linesDeleted++;
            this[filePath].LinesDeleted++;
        }

        private void IncrementLinesAdded(string filePath)
        {
            linesAdded++;
            this[filePath].LinesAdded++;
        }

        private int FileCallback(IntPtr data, GitDiffDelta delta, float progress)
        {
            AddFileChange(delta);

            return 0;
        }

        private void AddFileChange(GitDiffDelta delta)
        {
            var newFilePath = (string)marshaler.MarshalNativeToManaged(delta.NewFile.Path);
            var oldFilePath = (string)marshaler.MarshalNativeToManaged(delta.OldFile.Path);
            var newMode = (Mode)delta.NewFile.Mode;
            var oldMode = (Mode)delta.OldFile.Mode;

            var diffFile = new TreeEntryChanges(newFilePath, newMode, delta.Status, oldFilePath, oldMode);

            fileDispatcher[delta.Status](this, diffFile);
            changes.Add(diffFile.Path, diffFile);
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator<TreeEntryChanges> GetEnumerator()
        {
            return changes.Values.GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///   Gets the <see cref = "TreeEntryChanges"/> corresponding to the specified <paramref name = "path"/>.
        /// </summary>
        public TreeEntryChanges this[string path]
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
        ///   List of <see cref = "TreeEntryChanges"/> that have been been added.
        /// </summary>
        public IEnumerable<TreeEntryChanges> Added
        {
            get { return added; }
        }

        /// <summary>
        ///   List of <see cref = "TreeEntryChanges"/> that have been deleted.
        /// </summary>
        public IEnumerable<TreeEntryChanges> Deleted
        {
            get { return deleted; }
        }

        /// <summary>
        ///   List of <see cref = "TreeEntryChanges"/> that have been modified.
        /// </summary>
        public IEnumerable<TreeEntryChanges> Modified
        {
            get { return modified; }
        }

        /// <summary>
        ///   The total number of lines added in this diff.
        /// </summary>
        public int LinesAdded
        {
            get { return linesAdded; }
        }

        /// <summary>
        ///   The total number of lines added in this diff.
        /// </summary>
        public int LinesDeleted
        {
            get { return linesDeleted; }
        }

        /// <summary>
        ///   The full patch file of this diff.
        /// </summary>
        public string Patch
        {
            get { return patch; }
        }
    }
}
