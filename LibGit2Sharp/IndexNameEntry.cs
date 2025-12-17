using System;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A reference to the paths involved in a rename <see cref="Conflict"/>,
    /// known by the <see cref="Index"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class IndexNameEntry : IEquatable<IndexNameEntry>
    {
        private static readonly LambdaEqualityHelper<IndexNameEntry> equalityHelper =
            new LambdaEqualityHelper<IndexNameEntry>(x => x.Ancestor, x => x.Ours, x => x.Theirs);

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected IndexNameEntry()
        { }

        internal static unsafe IndexNameEntry BuildFromPtr(git_index_name_entry* entry)
        {
            if (entry == null)
            {
                return null;
            }

            string ancestor = entry->ancestor != null
                ? LaxFilePathMarshaler.FromNative(entry->ancestor).Native
                : null;
            string ours = entry->ours != null
                ? LaxFilePathMarshaler.FromNative(entry->ours).Native
                : null;
            string theirs = entry->theirs != null
                ? LaxFilePathMarshaler.FromNative(entry->theirs).Native
                : null;

            return new IndexNameEntry
            {
                Ancestor = ancestor,
                Ours = ours,
                Theirs = theirs,
            };
        }

        /// <summary>
        /// Gets the path of the ancestor side of the conflict.
        /// </summary>
        public virtual string Ancestor { get; private set; }

        /// <summary>
        /// Gets the path of the "ours" side of the conflict.
        /// </summary>
        public virtual string Ours { get; private set; }

        /// <summary>
        /// Gets the path of the "theirs" side of the conflict.
        /// </summary>
        public virtual string Theirs { get; private set; }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="IndexNameEntry"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="IndexNameEntry"/>.</param>
        /// <returns>True if the specified <see cref="object"/> is equal to the current <see cref="IndexNameEntry"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as IndexNameEntry);
        }

        /// <summary>
        /// Determines whether the specified <see cref="IndexNameEntry"/> is equal to the current <see cref="IndexNameEntry"/>.
        /// </summary>
        /// <param name="other">The <see cref="IndexNameEntry"/> to compare with the current <see cref="IndexNameEntry"/>.</param>
        /// <returns>True if the specified <see cref="IndexNameEntry"/> is equal to the current <see cref="IndexNameEntry"/>; otherwise, false.</returns>
        public bool Equals(IndexNameEntry other)
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
        /// Tests if two <see cref="IndexNameEntry"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="IndexNameEntry"/> to compare.</param>
        /// <param name="right">Second <see cref="IndexNameEntry"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(IndexNameEntry left, IndexNameEntry right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="IndexNameEntry"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="IndexNameEntry"/> to compare.</param>
        /// <param name="right">Second <see cref="IndexNameEntry"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(IndexNameEntry left, IndexNameEntry right)
        {
            return !Equals(left, right);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "{0} {1} {2}",
                                     Ancestor,
                                     Ours,
                                     Theirs);
            }
        }
    }
}
