using System;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A reference to a resolved <see cref="Conflict"/>,
    /// known by the <see cref="Index"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class IndexReucEntry : IEquatable<IndexReucEntry>
    {
        private static readonly LambdaEqualityHelper<IndexReucEntry> equalityHelper =
            new LambdaEqualityHelper<IndexReucEntry>(x => x.Path,
                                                     x => x.AncestorId, x => x.AncestorMode,
                                                     x => x.OurId, x => x.OurMode,
                                                     x => x.TheirId, x => x.TheirMode);

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected IndexReucEntry()
        { }

        internal static unsafe IndexReucEntry BuildFromPtr(git_index_reuc_entry* entry)
        {
            if (entry == null)
            {
                return null;
            }

            FilePath path = LaxUtf8Marshaler.FromNative(entry->Path);

            return new IndexReucEntry
            {
                Path = path.Native,
                AncestorId = ObjectId.BuildFromPtr(&entry->AncestorId),
                AncestorMode = (Mode)entry->AncestorMode,
                OurId = ObjectId.BuildFromPtr(&entry->OurId),
                OurMode = (Mode)entry->OurMode,
                TheirId = ObjectId.BuildFromPtr(&entry->TheirId),
                TheirMode = (Mode)entry->TheirMode,
            };
        }

        /// <summary>
        /// Gets the path of this conflict.
        /// </summary>
        public virtual string Path { get; private set; }

        /// <summary>
        /// Gets the <see cref="ObjectId"/> that was the ancestor of this
        /// conflict.
        /// </summary>
        public virtual ObjectId AncestorId { get; private set; }

        /// <summary>
        /// Gets the <see cref="Mode"/> of the file that was the ancestor of
        /// conflict.
        /// </summary>
        public virtual Mode AncestorMode { get; private set; }

        /// <summary>
        /// Gets the <see cref="ObjectId"/> that was "our" side of this
        /// conflict.
        /// </summary>
        public virtual ObjectId OurId { get; private set; }

        /// <summary>
        /// Gets the <see cref="Mode"/> of the file that was "our" side of
        /// the conflict.
        /// </summary>
        public virtual Mode OurMode { get; private set; }

        /// <summary>
        /// Gets the <see cref="ObjectId"/> that was "their" side of this
        /// conflict.
        /// </summary>
        public virtual ObjectId TheirId { get; private set; }

        /// <summary>
        /// Gets the <see cref="Mode"/> of the file that was "their" side of
        /// the conflict.
        /// </summary>
        public virtual Mode TheirMode { get; private set; }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="IndexReucEntry"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="IndexReucEntry"/>.</param>
        /// <returns>True if the specified <see cref="object"/> is equal to the current <see cref="IndexReucEntry"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as IndexReucEntry);
        }

        /// <summary>
        /// Determines whether the specified <see cref="IndexReucEntry"/> is equal to the current <see cref="IndexReucEntry"/>.
        /// </summary>
        /// <param name="other">The <see cref="IndexReucEntry"/> to compare with the current <see cref="IndexReucEntry"/>.</param>
        /// <returns>True if the specified <see cref="IndexReucEntry"/> is equal to the current <see cref="IndexReucEntry"/>; otherwise, false.</returns>
        public bool Equals(IndexReucEntry other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        /// Tests if two <see cref="IndexReucEntry"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="IndexReucEntry"/> to compare.</param>
        /// <param name="right">Second <see cref="IndexReucEntry"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(IndexReucEntry left, IndexReucEntry right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="IndexReucEntry"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="IndexReucEntry"/> to compare.</param>
        /// <param name="right">Second <see cref="IndexReucEntry"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(IndexReucEntry left, IndexReucEntry right)
        {
            return !Equals(left, right);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "{0}: {1} {2} {3}",
                                     Path,
                                     AncestorId,
                                     OurId,
                                     TheirId);
            }
        }
    }
}
