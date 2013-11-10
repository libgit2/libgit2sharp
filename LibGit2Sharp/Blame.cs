using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Strategy used for blaming.
    /// </summary>
    public enum BlameStrategy
    {
        /// <summary>
        /// Track renames of the file, but no block movement.
        /// </summary>
        Default,

        /// <summary>
        /// Track copies within the same file.
        /// </summary>
        [Obsolete("Not supported in libgit2 yet")]
        TrackCopiesSameFile,

        /// <summary>
        /// Track movement across files within the same commit.
        /// </summary>
        [Obsolete("Not supported in libgit2 yet")]
        TrackCopiesSameCommitMoves,

        /// <summary>
        /// Track copies across files within the same commit.
        /// </summary>
        [Obsolete("Not supported in libgit2 yet")]
        TrackCopiesSameCommitCopies,

        /// <summary>
        /// Track copies across all files in all commits.
        /// </summary>
        [Obsolete("Not supported in libgit2 yet")]
        TrackCopiesAnyCommitCopies
    }

    /// <summary>
    /// The result of a blame operation.
    /// </summary>
    public class Blame
    {
        private readonly IRepository repo;
        private readonly List<BlameHunk> hunks = new List<BlameHunk>(); 

        internal Blame(IRepository repo, BlameSafeHandle handle)
        {
            this.repo = repo;

            // Pre-fetch all the hunks
            var numHunks = NativeMethods.git_blame_get_hunk_count(handle);
            for (uint i = 0; i < numHunks; ++i)
            {
                var rawHunk = Proxy.git_blame_get_hunk_byindex(handle, i);
                hunks.Add(new BlameHunk(repo, rawHunk));
            }
        }

        /// <summary>
        /// For easy mocking
        /// </summary>
        protected Blame() { }

        /// <summary>
        /// Access blame hunks by index.
        /// </summary>
        /// <param name="idx">The 0-based index of the hunk to retrieve</param>
        /// <returns>The <see cref="BlameHunk"/> at the given index.</returns>
        public virtual BlameHunk this[int idx]
        {
            get { return hunks[idx]; }
        }

        /// <summary>
        /// Access blame hunks by the file line.
        /// </summary>
        /// <param name="line">1-based line number to </param>
        /// <returns>The <see cref="BlameHunk"/> for the specified file line.</returns>
        public virtual BlameHunk HunkForLine(int line)
        {
            var hunk = hunks.FirstOrDefault(x => x.ContainsLine(line));
            if (hunk != null)
            {
                return hunk;
            }
            throw new ArgumentOutOfRangeException("line", "No hunk for that line");
        }
    }
}
