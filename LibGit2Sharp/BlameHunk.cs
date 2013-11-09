using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp
{
    /// <summary>
    /// A contiguous group of lines that have been traced to a single commit.
    /// </summary>
    public class BlameHunk
    {
        private readonly IRepository repository;

        internal BlameHunk(IRepository repository, GitBlameHunk rawHunk)
        {
            this.repository = repository;

            finalCommit = new Lazy<Commit>(() => repository.Lookup<Commit>(rawHunk.FinalCommitId));
            origCommit = new Lazy<Commit>(() => repository.Lookup<Commit>(rawHunk.OrigCommitId));

            if (rawHunk.OrigPath != IntPtr.Zero)
            {
                OrigPath = LaxUtf8Marshaler.FromNative(rawHunk.OrigPath);
            }
            NumLines = rawHunk.LinesInHunk;
            FinalStartLineNumber = rawHunk.FinalStartLineNumber;
            OrigStartLineNumber = rawHunk.FinalStartLineNumber;

            // Signature objects need to have ownership of their native pointers
            if (rawHunk.FinalSignature != IntPtr.Zero)
            {
                FinalSignature = new Signature(NativeMethods.git_signature_dup(rawHunk.FinalSignature));
            }
            if (rawHunk.OrigSignature != IntPtr.Zero)
            {
                OrigSignature = new Signature(NativeMethods.git_signature_dup(rawHunk.OrigSignature));
            }
        }

        /// <summary>
        /// For easier mocking
        /// </summary>
        protected BlameHunk() { }

        /// <summary>
        /// Determine if this hunk contains a given line.
        /// </summary>
        /// <param name="line">1-based line number to test</param>
        /// <returns>True iff this hunk contains the given line.</returns>
        public virtual bool ContainsLine(int line)
        {
            return FinalStartLineNumber <= line && line < FinalStartLineNumber + NumLines;
        }

        /// <summary>
        /// Number of lines in this hunk.
        /// </summary>
        public virtual int NumLines { get; private set; }

        /// <summary>
        /// The 1-based line number where this hunk begins, as of <see cref="FinalCommit"/>
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
        /// 1-based line number where this hunk begins, as of <see cref="FinalCommit"/>, in <see cref="OrigPath"/>.
        /// </summary>
        public virtual int OrigStartLineNumber { get; private set; }

        /// <summary>
        /// Signature of the oldest-traced change to this hunk.
        /// </summary>
        public virtual Signature OrigSignature { get; private set; }

        /// <summary>
        /// Commit to which the oldest change to this hunk has been traced.
        /// </summary>
        public virtual Commit OrigCommit { get { return origCommit.Value; } }

        /// <summary>
        /// Path to the file where this hunk originated, as of <see cref="OrigCommit"/>.
        /// </summary>
        public virtual string OrigPath { get; private set; }

        private readonly Lazy<Commit> finalCommit;
        private readonly Lazy<Commit> origCommit;
    }
}
