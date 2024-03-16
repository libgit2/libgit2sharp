using System;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Holds the calculated status of a particular file at a particular instant.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class StatusEntry : IEquatable<StatusEntry>
    {
        private readonly string filePath;
        private readonly FileStatus state;
        private readonly RenameDetails headToIndexRenameDetails;
        private readonly RenameDetails indexToWorkDirRenameDetails;

        private static readonly LambdaEqualityHelper<StatusEntry> equalityHelper =
            new LambdaEqualityHelper<StatusEntry>(x => x.FilePath, x => x.State, x => x.HeadToIndexRenameDetails, x => x.IndexToWorkDirRenameDetails);

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected StatusEntry()
        { }

        internal StatusEntry(string filePath, FileStatus state, RenameDetails headToIndexRenameDetails = null, RenameDetails indexToWorkDirRenameDetails = null)
        {
            this.filePath = filePath;
            this.state = state;
            this.headToIndexRenameDetails = headToIndexRenameDetails;
            this.indexToWorkDirRenameDetails = indexToWorkDirRenameDetails;
        }

        /// <summary>
        /// Gets the <see cref="FileStatus"/> of the file.
        /// </summary>
        public virtual FileStatus State
        {
            get { return state; }
        }

        /// <summary>
        /// Gets the relative new filepath to the working directory of the file.
        /// </summary>
        public virtual string FilePath
        {
            get { return filePath; }
        }

        /// <summary>
        /// Gets the rename details from the HEAD to the Index, if this <see cref="FileStatus"/> contains <see cref="FileStatus.RenamedInIndex"/>
        /// </summary>
        public virtual RenameDetails HeadToIndexRenameDetails
        {
            get { return headToIndexRenameDetails; }
        }

        /// <summary>
        /// Gets the rename details from the Index to the working directory, if this <see cref="FileStatus"/> contains <see cref="FileStatus.RenamedInWorkdir"/>
        /// </summary>
        public virtual RenameDetails IndexToWorkDirRenameDetails
        {
            get { return indexToWorkDirRenameDetails; }
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="StatusEntry"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="StatusEntry"/>.</param>
        /// <returns>True if the specified <see cref="object"/> is equal to the current <see cref="StatusEntry"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as StatusEntry);
        }

        /// <summary>
        /// Determines whether the specified <see cref="StatusEntry"/> is equal to the current <see cref="StatusEntry"/>.
        /// </summary>
        /// <param name="other">The <see cref="StatusEntry"/> to compare with the current <see cref="StatusEntry"/>.</param>
        /// <returns>True if the specified <see cref="StatusEntry"/> is equal to the current <see cref="StatusEntry"/>; otherwise, false.</returns>
        public bool Equals(StatusEntry other)
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
        /// Tests if two <see cref="StatusEntry"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="StatusEntry"/> to compare.</param>
        /// <param name="right">Second <see cref="StatusEntry"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(StatusEntry left, StatusEntry right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="StatusEntry"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="StatusEntry"/> to compare.</param>
        /// <param name="right">Second <see cref="StatusEntry"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(StatusEntry left, StatusEntry right)
        {
            return !Equals(left, right);
        }

        private string DebuggerDisplay
        {
            get
            {
                if ((State & FileStatus.RenamedInIndex) == FileStatus.RenamedInIndex ||
                    (State & FileStatus.RenamedInWorkdir) == FileStatus.RenamedInWorkdir)
                {
                    string oldFilePath = ((State & FileStatus.RenamedInIndex) != 0)
                        ? HeadToIndexRenameDetails.OldFilePath
                        : IndexToWorkDirRenameDetails.OldFilePath;

                    return string.Format(CultureInfo.InvariantCulture, "{0}: {1} -> {2}", State, oldFilePath, FilePath);
                }

                return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", State, FilePath);
            }
        }
    }
}
