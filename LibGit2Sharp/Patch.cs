using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Holds the patch between two trees.
    /// <para>The individual patches for each file can be accessed through the indexer of this class.</para>
    /// <para>Building a patch is an expensive operation. If you only need to know which files have been added,
    /// deleted, modified, ..., then consider using a simpler <see cref="TreeChanges"/>.</para>
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Patch : IEnumerable<ContentChanges>
    {
        private readonly StringBuilder fullPatchBuilder = new StringBuilder();

        private readonly IDictionary<FilePath, ContentChanges> changes = new Dictionary<FilePath, ContentChanges>();
        private int linesAdded;
        private int linesDeleted;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Patch()
        { }

        internal Patch(DiffSafeHandle diff)
        {
            int count = Proxy.git_diff_num_deltas(diff);
            for (int i = 0; i < count; i++)
            {
                using (var patch = Proxy.git_patch_from_diff(diff, i))
                {
                    var delta = Proxy.git_diff_get_delta(diff, i);
                    AddFileChange(delta);
                    Proxy.git_patch_print(patch, PrintCallBack);
                }

            }
        }

        private void AddFileChange(GitDiffDelta delta)
        {
            var pathPtr = delta.NewFile.Path != IntPtr.Zero ? delta.NewFile.Path : delta.OldFile.Path;
            var newFilePath = LaxFilePathMarshaler.FromNative(pathPtr);
            changes.Add(newFilePath, new ContentChanges(delta.IsBinary()));
        }

        private int PrintCallBack(GitDiffDelta delta, GitDiffHunk hunk, GitDiffLine line, IntPtr payload)
        {
            string patchPart = LaxUtf8Marshaler.FromNative(line.content, (int)line.contentLen);

            // Deleted files mean no "new file" path

            var pathPtr = delta.NewFile.Path != IntPtr.Zero ? delta.NewFile.Path : delta.OldFile.Path;
            var filePath = LaxFilePathMarshaler.FromNative(pathPtr);

            ContentChanges currentChange = this[filePath];
            string prefix = string.Empty;

            switch (line.lineOrigin)
            {
                case GitDiffLineOrigin.GIT_DIFF_LINE_CONTEXT:
                    prefix = " ";
                    break;

                case GitDiffLineOrigin.GIT_DIFF_LINE_ADDITION:
                    linesAdded++;
                    currentChange.LinesAdded++;
                    prefix = "+";
                    break;

                case GitDiffLineOrigin.GIT_DIFF_LINE_DELETION:
                    linesDeleted++;
                    currentChange.LinesDeleted++;
                    prefix = "-";
                    break;
            }

            string formattedOutput = string.Concat(prefix, patchPart);

            fullPatchBuilder.Append(formattedOutput);
            currentChange.AppendToPatch(formattedOutput);

            return 0;
        }

        #region IEnumerable<ContentChanges> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<ContentChanges> GetEnumerator()
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
        /// Gets the <see cref="ContentChanges"/> corresponding to the specified <paramref name="path"/>.
        /// </summary>
        public virtual ContentChanges this[string path]
        {
            get { return this[(FilePath)path]; }
        }

        private ContentChanges this[FilePath path]
        {
            get
            {
                ContentChanges contentChanges;
                if (changes.TryGetValue(path, out contentChanges))
                {
                    return contentChanges;
                }

                return null;
            }
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
        public virtual string Content
        {
            get { return fullPatchBuilder.ToString(); }
        }

        /// <summary>
        /// Implicit operator for string conversion.
        /// </summary>
        /// <param name="patch"><see cref="Patch"/>.</param>
        /// <returns>The patch content as string.</returns>
        public static implicit operator string(Patch patch)
        {
            return patch.fullPatchBuilder.ToString();
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "+{0} -{1}", linesAdded, linesDeleted);
            }
        }
    }
}
