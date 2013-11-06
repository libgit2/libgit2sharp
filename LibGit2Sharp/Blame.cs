using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
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
        private readonly IRepository repo;
        private readonly BlameSafeHandle handle;
        private readonly List<BlameHunk> hunks = new List<BlameHunk>(); 

        internal Blame(IRepository repo, BlameSafeHandle handle)
        {
            this.repo = repo;
            this.handle = handle;

            // Pre-fetch all the hunks
            var numHunks = NativeMethods.git_blame_get_hunk_count(handle);
            for (uint i = 0; i < numHunks; ++i)
            {
                var rawHunk = NativeMethods.git_blame_get_hunk_byindex(handle, i);
                hunks.Add(new BlameHunk(repo, rawHunk));
            }
        }

        /// <summary>
        /// For easy mocking
        /// </summary>
        protected Blame() { }

        public virtual BlameHunk this[int idx]
        {
            get { return hunks[idx]; }
        }

        public virtual BlameHunk HunkForLine(uint line)
        {
            var hunk = hunks.FirstOrDefault(x => x.ContainsLine(line));
            if (hunk != null) return hunk;
            throw new ArgumentOutOfRangeException("line", "No hunk for that line");
        }
    }
}