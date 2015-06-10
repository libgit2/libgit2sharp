using System;
using System.Globalization;
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

        /// <summary>
        /// Restrict the search of commits to those reachable
        /// following only the first parents.
        /// </summary>
        GIT_BLAME_FIRST_PARENT = (1 << 4),
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitBlameOptions
    {
        public uint version = 1;
        public GitDiffFindOptions FindOptions;
        public GitBlameOptionFlags flags;
        public UInt16 MinMatchCharacters;
        public GitOid NewestCommit;
        public GitOid OldestCommit;
        public uint MinLine;
        public uint MaxLine;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitBlameHunk
    {
        public ushort LinesInHunk;

        public GitOid FinalCommitId;
        public ushort FinalStartLineNumber;
        public IntPtr FinalSignature;

        public GitOid OrigCommitId;
        public IntPtr OrigPath;
        public ushort OrigStartLineNumber;
        public IntPtr OrigSignature;

        public byte Boundary;
    }

    internal static class BlameStrategyExtensions
    {
        public static GitBlameOptionFlags ToGitBlameOptionFlags(this BlameStrategy strategy)
        {
            switch (strategy)
            {
                case BlameStrategy.Default:
                    return GitBlameOptionFlags.GIT_BLAME_NORMAL;

                default:
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture,
                                                                  "{0} is not supported at this time",
                                                                  strategy));
            }
        }
    }
}
