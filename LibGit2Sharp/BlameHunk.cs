using System;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A contiguous group of lines that have been traced to a single commit.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class BlameHunk : IEquatable<BlameHunk>
    {
        private static readonly LambdaEqualityHelper<BlameHunk> equalityHelper =
            new LambdaEqualityHelper<BlameHunk>(x => x.LineCount,
                                                x => x.FinalStartLineNumber,
                                                x => x.FinalSignature,
                                                x => x.InitialStartLineNumber,
                                                x => x.InitialSignature,
                                                x => x.InitialCommit);

        internal unsafe BlameHunk(IRepository repository, git_blame_hunk* rawHunk)
        {
            var origId = ObjectId.BuildFromPtr(&rawHunk->orig_commit_id);
            var finalId = ObjectId.BuildFromPtr(&rawHunk->final_commit_id);

            finalCommit = new Lazy<Commit>(() => repository.Lookup<Commit>(finalId));
            origCommit = new Lazy<Commit>(() => repository.Lookup<Commit>(origId));


            if (rawHunk->orig_path != null)
            {
                InitialPath = LaxUtf8Marshaler.FromNative(rawHunk->orig_path);
            }

            LineCount = (int)rawHunk->lines_in_hunk.ToUInt32();

            // Libgit2's line numbers are 1-based
            FinalStartLineNumber = (int)rawHunk->final_start_line_number.ToUInt32() - 1;
            InitialStartLineNumber = (int)rawHunk->orig_start_line_number.ToUInt32() - 1;

            // Signature objects need to have ownership of their native pointers
            if (rawHunk->final_signature != null)
            {
                FinalSignature = new Signature(rawHunk->final_signature);
            }

            if (rawHunk->orig_signature != null)
            {
                InitialSignature = new Signature(rawHunk->orig_signature);
            }
        }

        /// <summary>
        /// For easier mocking
        /// </summary>
        protected BlameHunk()
        { }

        /// <summary>
        /// Determine if this hunk contains a given line.
        /// </summary>
        /// <param name="line">Line number to test</param>
        /// <returns>True if this hunk contains the given line.</returns>
        public virtual bool ContainsLine(int line)
        {
            return FinalStartLineNumber <= line && line < FinalStartLineNumber + LineCount;
        }

        /// <summary>
        /// Number of lines in this hunk.
        /// </summary>
        public virtual int LineCount { get; private set; }

        /// <summary>
        /// The line number where this hunk begins, as of <see cref="FinalCommit"/>
        /// </summary>
        public virtual int FinalStartLineNumber { get; private set; }

        /// <summary>
        /// Signature of the most recent change to this hunk.
        /// </summary>
        public virtual Signature FinalSignature { get; private set; }

        /// <summary>
        /// Commit which most recently changed this file.
        /// </summary>
        public virtual Commit FinalCommit { get { return finalCommit.Value; } }

        /// <summary>
        /// Line number where this hunk begins, as of <see cref="FinalCommit"/>, in <see cref="InitialPath"/>.
        /// </summary>
        public virtual int InitialStartLineNumber { get; private set; }

        /// <summary>
        /// Signature of the oldest-traced change to this hunk.
        /// </summary>
        public virtual Signature InitialSignature { get; private set; }

        /// <summary>
        /// Commit to which the oldest change to this hunk has been traced.
        /// </summary>
        public virtual Commit InitialCommit { get { return origCommit.Value; } }

        /// <summary>
        /// Path to the file where this hunk originated, as of <see cref="InitialCommit"/>.
        /// </summary>
        public virtual string InitialPath { get; private set; }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "{0}-{1} ({2})",
                                     FinalStartLineNumber,
                                     FinalStartLineNumber+LineCount-1,
                                     FinalCommit.ToString().Substring(0,7));
            }
        }

        private readonly Lazy<Commit> finalCommit;
        private readonly Lazy<Commit> origCommit;

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(BlameHunk other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="BlameHunk"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="BlameHunk"/>.</param>
        /// <returns>True if the specified <see cref="object"/> is equal to the current <see cref="BlameHunk"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as BlameHunk);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode();
        }

        /// <summary>
        /// Tests if two <see cref="BlameHunk"/>s are equal.
        /// </summary>
        /// <param name="left">First hunk to compare.</param>
        /// <param name="right">Second hunk to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(BlameHunk left, BlameHunk right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="BlameHunk"/>s are unequal.
        /// </summary>
        /// <param name="left">First hunk to compare.</param>
        /// <param name="right">Second hunk to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(BlameHunk left, BlameHunk right)
        {
            return !Equals(left, right);
        }
    }
}
