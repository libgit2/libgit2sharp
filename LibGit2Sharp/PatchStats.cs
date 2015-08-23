using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Holds summary information for a diff.
    /// <para>The individual patches for each file can be accessed through the indexer of this class.</para>
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class PatchStats : IEnumerable<ContentChangeStats>, IDiffResult
    {
        private readonly IDictionary<FilePath, ContentChangeStats> changes = new Dictionary<FilePath, ContentChangeStats>();
        private readonly int totalLinesAdded;
        private readonly int totalLinesDeleted;

        /// <summary>
        /// For mocking.
        /// </summary>
        protected PatchStats()
        { }

        internal PatchStats(DiffSafeHandle diff)
        {
            int count = Proxy.git_diff_num_deltas(diff);
            for (int i = 0; i < count; i++)
            {
                using (var patch = Proxy.git_patch_from_diff(diff, i))
                {
                    var delta = Proxy.git_diff_get_delta(diff, i);
                    var pathPtr = delta.NewFile.Path != IntPtr.Zero ? delta.NewFile.Path : delta.OldFile.Path;
                    var newFilePath = LaxFilePathMarshaler.FromNative(pathPtr);

                    var stats = Proxy.git_patch_line_stats(patch);
                    int added = stats.Item1;
                    int deleted = stats.Item2;
                    changes.Add(newFilePath, new ContentChangeStats(added, deleted));
                    totalLinesAdded += added;
                    totalLinesDeleted += deleted;
                }

            }
        }

        #region IEnumerable<ContentChanges> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<ContentChangeStats> GetEnumerator()
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
        /// Gets the <see cref="ContentChangeStats"/> corresponding to the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path"></param>
        public virtual ContentChangeStats this[string path]
        {
            get { return this[(FilePath)path]; }
        }

        private ContentChangeStats this[FilePath path]
        {
            get
            {
                ContentChangeStats stats;
                if (changes.TryGetValue(path, out stats))
                {
                    return stats;
                }
                return null;
            }
        }

        /// <summary>
        /// The total number of lines added in this diff.
        /// </summary>
        public virtual int TotalLinesAdded
        {
            get { return totalLinesAdded; }
        }

        /// <summary>
        /// The total number of lines deleted in this diff.
        /// </summary>
        public virtual int TotalLinesDeleted
        {
            get { return totalLinesDeleted; }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "+{0} -{1}",
                                     TotalLinesAdded,
                                     TotalLinesDeleted);
            }
        }
    }
}
