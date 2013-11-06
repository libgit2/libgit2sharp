using System;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    public enum BlameStrategy
    {
        Normal,
        TrackCopiesSameFile,
        TrackCopiesSameCommitMoves,
        TrackCopiesSameCommitCopies,
        TrackCopiesAnyCommitCopies
    }

    public class Blame
    {
        public class Hunk
        {
            private readonly IRepository repository;
            private readonly GitBlameHunk rawHunk;

            internal Hunk(IRepository repository, GitBlameHunk rawHunk)
            {
                this.repository = repository;
                this.rawHunk = rawHunk;

                finalCommit = new Lazy<Commit>(() => repository.Lookup<Commit>(rawHunk.FinalCommitId));
                origCommit = new Lazy<Commit>(() => repository.Lookup<Commit>(rawHunk.OrigCommitId));

                // Signature objects need to have ownership of their native pointers
                if (rawHunk.FinalSignature != IntPtr.Zero)
                    FinalSignature = new Signature(NativeMethods.git_signature_dup(rawHunk.FinalSignature));
                if (rawHunk.OrigSignature != IntPtr.Zero)
                    origSignature = new Signature(NativeMethods.git_signature_dup(rawHunk.OrigSignature));
            }

            public int NumLines { get { return rawHunk.LinesInHunk; } }

            public int FinalStartLineNumber { get { return rawHunk.FinalStartLineNumber; } }
            public Signature FinalSignature { get; private set; }
            public Commit FinalCommit { get { return finalCommit.Value; } }

            public int origStartLineNumber { get { return rawHunk.OrigStartLineNumber; } }
            public Signature origSignature { get; private set; }
            public Commit OrigCommit { get { return origCommit.Value; } }

            public string OrigPath
            {
                get
                {
                    return new StrictUtf8Marshaler().MarshalNativeToManaged(rawHunk.OrigPath) as string;
                }
            }

            private readonly Lazy<Commit> finalCommit;
            private readonly Lazy<Commit> origCommit;
        }

        private readonly IRepository repo;
        private readonly BlameSafeHandle handle;

        internal Blame(IRepository repo, BlameSafeHandle handle)
        {
            this.repo = repo;
            this.handle = handle;
        }

        public Hunk this[int idx]
        {
            get
            {
                var rawHunk = NativeMethods.git_blame_get_hunk_byindex(handle, (UInt32)idx);
                return new Hunk(repo, rawHunk);
            }
        }
    }
}