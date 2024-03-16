using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///  Represents a group of index entries that describe a merge conflict
    ///  in the index.  This is typically a set of ancestor, ours and theirs
    ///  entries for a given path.
    ///
    /// Any side may be missing to reflect additions or deletions in the
    /// branches being merged.
    /// </summary>
    public class Conflict : IEquatable<Conflict>
    {
        private readonly IndexEntry ancestor;
        private readonly IndexEntry ours;
        private readonly IndexEntry theirs;

        private static readonly LambdaEqualityHelper<Conflict> equalityHelper =
            new LambdaEqualityHelper<Conflict>(x => x.Ancestor, x => x.Ours, x => x.Theirs);

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Conflict()
        { }

        internal Conflict(IndexEntry ancestor, IndexEntry ours, IndexEntry theirs)
        {
            this.ancestor = ancestor;
            this.ours = ours;
            this.theirs = theirs;
        }

        /// <summary>
        ///  The index entry of the ancestor side of the conflict (the stage
        ///  1 index entry.)
        /// </summary>
        public virtual IndexEntry Ancestor
        {
            get { return ancestor; }
        }

        /// <summary>
        ///  The index entry of the "ours" (ORIG_HEAD or merge target) side
        ///  of the conflict (the stage 2 index entry.)
        /// </summary>
        public virtual IndexEntry Ours
        {
            get { return ours; }
        }

        /// <summary>
        ///  The index entry of the "theirs" (merge source) side of the
        ///  conflict (the stage 3 index entry.)
        /// </summary>
        public virtual IndexEntry Theirs
        {
            get { return theirs; }
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is
        /// equal to the current <see cref="Conflict"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with
        /// the current <see cref="Conflict"/>.</param>
        /// <returns>true if the specified <see cref="object"/> is equal
        /// to the current <see cref="Conflict"/>; otherwise,
        /// false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Conflict);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Conflict"/>
        /// is equal to the current <see cref="Conflict"/>.
        /// </summary>
        /// <param name="other">The <see cref="Conflict"/> to compare
        /// with the current <see cref="Conflict"/>.</param>
        /// <returns>true if the specified <see cref="Conflict"/> is equal
        /// to the current <see cref="Conflict"/>; otherwise,
        /// false.</returns>
        public bool Equals(Conflict other)
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
        /// Tests if two <see cref="Conflict"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="Conflict"/> to compare.</param>
        /// <param name="right">Second <see cref="Conflict"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Conflict left, Conflict right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="Conflict"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="Conflict"/> to compare.</param>
        /// <param name="right">Second <see cref="Conflict"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Conflict left, Conflict right)
        {
            return !Equals(left, right);
        }
    }
}
