using System;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Holds the rename details of a particular file.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class RenameDetails : IEquatable<RenameDetails>
    {
        private readonly string oldFilePath;
        private readonly string newFilePath;
        private readonly int similarity;

        private static readonly LambdaEqualityHelper<RenameDetails> equalityHelper =
            new LambdaEqualityHelper<RenameDetails>(x => x.OldFilePath, x => x.NewFilePath, x => x.Similarity);

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected RenameDetails()
        { }

        internal RenameDetails(string oldFilePath, string newFilePath, int similarity)
        {
            this.oldFilePath = oldFilePath;
            this.newFilePath = newFilePath;
            this.similarity = similarity;
        }

        /// <summary>
        /// Gets the relative filepath to the working directory of the old file (the rename source).
        /// </summary>
        public virtual string OldFilePath
        {
            get { return oldFilePath; }
        }

        /// <summary>
        /// Gets the relative filepath to the working directory of the new file (the rename target).
        /// </summary>
        public virtual string NewFilePath
        {
            get { return newFilePath; }
        }

        /// <summary>
        /// Gets the similarity between the old file an the new file (0-100).
        /// </summary>
        public virtual int Similarity
        {
            get { return similarity; }
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="RenameDetails"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="RenameDetails"/>.</param>
        /// <returns>True if the specified <see cref="object"/> is equal to the current <see cref="RenameDetails"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as RenameDetails);
        }

        /// <summary>
        /// Determines whether the specified <see cref="RenameDetails"/> is equal to the current <see cref="RenameDetails"/>.
        /// </summary>
        /// <param name="other">The <see cref="RenameDetails"/> to compare with the current <see cref="RenameDetails"/>.</param>
        /// <returns>True if the specified <see cref="RenameDetails"/> is equal to the current <see cref="RenameDetails"/>; otherwise, false.</returns>
        public bool Equals(RenameDetails other)
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
        /// Tests if two <see cref="RenameDetails"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="RenameDetails"/> to compare.</param>
        /// <param name="right">Second <see cref="RenameDetails"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(RenameDetails left, RenameDetails right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="RenameDetails"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="RenameDetails"/> to compare.</param>
        /// <param name="right">Second <see cref="RenameDetails"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(RenameDetails left, RenameDetails right)
        {
            return !Equals(left, right);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "{0} -> {1} [{2}%]",
                                     OldFilePath,
                                     NewFilePath,
                                     Similarity);
            }
        }
    }
}
