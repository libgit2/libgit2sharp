using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp
{
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

        public virtual bool ContainsLine(int line)
        {
            return FinalStartLineNumber <= line && line < FinalStartLineNumber + NumLines;
        }

        public virtual int NumLines { get; private set; }

        public virtual int FinalStartLineNumber { get; private set; }
        public virtual Signature FinalSignature { get; private set; }
        public virtual Commit FinalCommit { get { return finalCommit.Value; } }

        public virtual int OrigStartLineNumber { get; private set; }
        public virtual Signature OrigSignature { get; private set; }
        public virtual Commit OrigCommit { get { return origCommit.Value; } }

        public virtual string OrigPath { get; private set; }

        private readonly Lazy<Commit> finalCommit;
        private readonly Lazy<Commit> origCommit;
    }
}
