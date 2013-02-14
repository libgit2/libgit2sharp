﻿using System;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A reference to a <see cref = "Blob" /> known by the <see cref = "Index" />.
    /// </summary>
    [DebuggerDisplayAttribute("{DebuggerDisplay,nq}")]
    public class IndexEntry : IEquatable<IndexEntry>
    {
        private static readonly LambdaEqualityHelper<IndexEntry> equalityHelper =
            new LambdaEqualityHelper<IndexEntry>(x => x.Path, x => x.Id, x => x.Mode, x => x.StageLevel);

        private Func<FileStatus> state;

        /// <summary>
        ///   State of the version of the <see cref = "Blob" /> pointed at by this <see cref = "IndexEntry" />,
        ///   compared against the <see cref = "Blob" /> known from the <see cref = "Repository.Head" /> and the file in the working directory.
        /// </summary>
        [Obsolete("This method will be removed in the next release. Please use Repository.Index.RetrieveStatus(filePath) overload instead.")]
        public virtual FileStatus State
        {
            get { return state(); }
        }

        /// <summary>
        ///   Gets the relative path to the file within the working directory.
        /// </summary>
        public virtual string Path { get; private set; }

        /// <summary>
        ///   Gets the file mode.
        /// </summary>
        public virtual Mode Mode { get; private set; }

        /// <summary>
        ///   Gets the stage number.
        /// </summary>
        public virtual StageLevel StageLevel { get; private set; }

        /// <summary>
        ///   Gets the id of the <see cref = "Blob" /> pointed at by this index entry.
        /// </summary>
        public virtual ObjectId Id { get; private set; }

        internal static IndexEntry BuildFromPtr(Repository repo, IndexEntrySafeHandle handle)
        {
            if (handle == null || handle.IsZero)
            {
                return null;
            }

            GitIndexEntry entry = handle.MarshalAsGitIndexEntry();

            var path = FilePathMarshaler.FromNative(entry.Path);

            return new IndexEntry
                       {
                           Path = path.Native,
                           Id = new ObjectId(entry.oid),
                           state = () => repo.Index.RetrieveStatus(path.Native),
                           StageLevel = Proxy.git_index_entry_stage(handle),
                           Mode = (Mode)entry.Mode
                       };
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "Object" /> is equal to the current <see cref = "IndexEntry" />.
        /// </summary>
        /// <param name = "obj">The <see cref = "Object" /> to compare with the current <see cref = "IndexEntry" />.</param>
        /// <returns>True if the specified <see cref = "Object" /> is equal to the current <see cref = "IndexEntry" />; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as IndexEntry);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "IndexEntry" /> is equal to the current <see cref = "IndexEntry" />.
        /// </summary>
        /// <param name = "other">The <see cref = "IndexEntry" /> to compare with the current <see cref = "IndexEntry" />.</param>
        /// <returns>True if the specified <see cref = "IndexEntry" /> is equal to the current <see cref = "IndexEntry" />; otherwise, false.</returns>
        public bool Equals(IndexEntry other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        ///   Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        ///   Tests if two <see cref = "IndexEntry" /> are equal.
        /// </summary>
        /// <param name = "left">First <see cref = "IndexEntry" /> to compare.</param>
        /// <param name = "right">Second <see cref = "IndexEntry" /> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(IndexEntry left, IndexEntry right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Tests if two <see cref = "IndexEntry" /> are different.
        /// </summary>
        /// <param name = "left">First <see cref = "IndexEntry" /> to compare.</param>
        /// <param name = "right">Second <see cref = "IndexEntry" /> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(IndexEntry left, IndexEntry right)
        {
            return !Equals(left, right);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} => \"{1}\"", Path, Id.ToString(7));
            }
        }
    }
}
