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
    internal class git_blame_options
    {
        public uint version = 1;
        public GitBlameOptionFlags flags;

        public ushort min_match_characters;
        public git_oid newest_commit;
        public git_oid oldest_commit;
        public UIntPtr min_line;
        public UIntPtr max_line;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_blame_hunk
    {
        public UIntPtr lines_in_hunk;

        public git_oid final_commit_id;
        public UIntPtr final_start_line_number;
        public git_signature* final_signature;

        public git_oid orig_commit_id;
        public char* orig_path;
        public UIntPtr orig_start_line_number;
        public git_signature* orig_signature;

        public byte boundary;
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
