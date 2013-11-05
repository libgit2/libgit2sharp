using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum GitBlameOptionFlags
    {
        /// <summary>
        /// Normal blame, the default
        /// </summary>
        GIT_BLAME_NORMAL = 0,

        /// <summary>
        /// Track lines that have moved within a file (like `git blame -M`).
        /// </summary>
        GIT_BLAME_TRACK_COPIES_SAME_FILE = (1 << 0),

        /** Track lines that have moved across files in the same commit (like `git blame -C`).
         * NOT IMPLEMENTED. */
        GIT_BLAME_TRACK_COPIES_SAME_COMMIT_MOVES = (1 << 1),

        /// <summary>
        /// Track lines that have been copied from another file that exists in the
        /// same commit (like `git blame -CC`). Implies SAME_FILE.
        /// </summary>
        GIT_BLAME_TRACK_COPIES_SAME_COMMIT_COPIES = (1 << 2),

        /// <summary>
        /// Track lines that have been copied from another file that exists in *any*
        /// commit (like `git blame -CCC`). Implies SAME_COMMIT_COPIES.
        /// </summary>
        GIT_BLAME_TRACK_COPIES_ANY_COMMIT_COPIES = (1 << 3),
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitBlameOptions
    {
        public uint version = 1;
        public GitBlameOptionFlags flags;
        public ushort MinMatchCharacters;
        public ObjectId NewestCommit;
        public ObjectId OldestCommit;
        public uint MinLine;
        public uint MaxLine;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitBlameHunk
    {
        public ushort LinesInHunk;
        
        public ObjectId FinalCommitId;
        public ushort FinalStartLineNumber;
        public GitSignature FinalSignature;

        public ObjectId OrigCommitId;
        public string OrigPath;
        public ushort OrigStartLineNumber;
        public GitSignature OrigSignature;

        public byte Boundary;
    }
}