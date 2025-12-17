using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum GitDiffOptionFlags
    {
        /// <summary>
        /// Normal diff, the default
        /// </summary>
        GIT_DIFF_NORMAL = 0,

        /*
         * Options controlling which files will be in the diff
         */

        /// <summary>
        /// Reverse the sides of the diff
        /// </summary>
        GIT_DIFF_REVERSE = (1 << 0),

        /// <summary>
        /// Include ignored files in the diff
        /// </summary>
        GIT_DIFF_INCLUDE_IGNORED = (1 << 1),

        /// <summary>
        /// Even with GIT_DIFF_INCLUDE_IGNORED, an entire ignored directory
        /// will be marked with only a single entry in the diff; this flag
        /// adds all files under the directory as IGNORED entries, too.
        /// </summary>
        GIT_DIFF_RECURSE_IGNORED_DIRS = (1 << 2),

        /// <summary>
        /// Include untracked files in the diff
        /// </summary>
        GIT_DIFF_INCLUDE_UNTRACKED = (1 << 3),

        /// <summary>
        /// Even with GIT_DIFF_INCLUDE_UNTRACKED, an entire untracked
        /// directory will be marked with only a single entry in the diff
        /// (a la what core Git does in `git status`); this flag adds *all*
        /// files under untracked directories as UNTRACKED entries, too.
        /// </summary>
        GIT_DIFF_RECURSE_UNTRACKED_DIRS = (1 << 4),

        /// <summary>
        /// Include unmodified files in the diff
        /// </summary>
        GIT_DIFF_INCLUDE_UNMODIFIED = (1 << 5),

        /// <summary>
        /// Normally, a type change between files will be converted into a
        /// DELETED record for the old and an ADDED record for the new; this
        /// options enabled the generation of TYPECHANGE delta records.
        /// </summary>
        GIT_DIFF_INCLUDE_TYPECHANGE = (1 << 6),

        /// <summary>
        /// Even with GIT_DIFF_INCLUDE_TYPECHANGE, blob->tree changes still
        /// generally show as a DELETED blob.  This flag tries to correctly
        /// label blob->tree transitions as TYPECHANGE records with new_file's
        /// mode set to tree.  Note: the tree SHA will not be available.
        /// </summary>
        GIT_DIFF_INCLUDE_TYPECHANGE_TREES = (1 << 7),

        /// <summary>
        /// Ignore file mode changes
        /// </summary>
        GIT_DIFF_IGNORE_FILEMODE = (1 << 8),

        /// <summary>
        /// Treat all submodules as unmodified
        /// </summary>
        GIT_DIFF_IGNORE_SUBMODULES = (1 << 9),

        /// <summary>
        /// Use case insensitive filename comparisons
        /// </summary>
        GIT_DIFF_IGNORE_CASE = (1 << 10),


        /// <summary>
        /// May be combined with `GIT_DIFF_IGNORE_CASE` to specify that a file
        /// that has changed case will be returned as an add/delete pair.
        /// </summary>
        GIT_DIFF_INCLUDE_CASECHANGE = (1 << 11),

        /// <summary>
        /// If the pathspec is set in the diff options, this flags means to
        /// apply it as an exact match instead of as an fnmatch pattern.
        /// </summary>
        GIT_DIFF_DISABLE_PATHSPEC_MATCH = (1 << 12),

        /// <summary>
        /// Disable updating of the `binary` flag in delta records.  This is
        /// useful when iterating over a diff if you don't need hunk and data
        /// callbacks and want to avoid having to load file completely.
        /// </summary>
        GIT_DIFF_SKIP_BINARY_CHECK = (1 << 13),

        /// <summary>
        /// When diff finds an untracked directory, to match the behavior of
        /// core Git, it scans the contents for IGNORED and UNTRACKED files.
        /// If *all* contents are IGNORED, then the directory is IGNORED; if
        /// any contents are not IGNORED, then the directory is UNTRACKED.
        /// This is extra work that may not matter in many cases.  This flag
        /// turns off that scan and immediately labels an untracked directory
        /// as UNTRACKED (changing the behavior to not match core Git).
        /// </summary>
        GIT_DIFF_ENABLE_FAST_UNTRACKED_DIRS = (1 << 14),

        /// <summary>
        /// When diff finds a file in the working directory with stat
        /// information different from the index, but the OID ends up being the
        /// same, write the correct stat information into the index.  Note:
        /// without this flag, diff will always leave the index untouched.
        /// </summary>
        GIT_DIFF_UPDATE_INDEX = (1 << 15),

        /// <summary>
        /// Include unreadable files in the diff
        /// </summary>
        GIT_DIFF_INCLUDE_UNREADABLE = (1 << 16),

        /// <summary>
        /// Include unreadable files in the diff
        /// </summary>
        GIT_DIFF_INCLUDE_UNREADABLE_AS_UNTRACKED = (1 << 17),

        /*
         * Options controlling how output will be generated
         */

        /// <summary>
        /// Use a heuristic that takes indentation and whitespace into account
        /// which generally can produce better diffs when dealing with ambiguous
        /// diff hunks.
        /// </summary>
        GIT_DIFF_INDENT_HEURISTIC = (1 << 18),

        /// <summary>
        /// Treat all files as text, disabling binary attributes and detection
        /// </summary>
        GIT_DIFF_FORCE_TEXT = (1 << 20),

        /// <summary>
        /// Treat all files as binary, disabling text diffs
        /// </summary>
        GIT_DIFF_FORCE_BINARY = (1 << 21),

        /// <summary>
        /// Ignore all whitespace
        /// </summary>
        GIT_DIFF_IGNORE_WHITESPACE = (1 << 22),

        /// <summary>
        /// Ignore changes in amount of whitespace
        /// </summary>
        GIT_DIFF_IGNORE_WHITESPACE_CHANGE = (1 << 23),

        /// <summary>
        /// Ignore whitespace at end of line
        /// </summary>
        GIT_DIFF_IGNORE_WHITESPACE_EOL = (1 << 24),

        /// <summary>
        /// When generating patch text, include the content of untracked
        /// files.  This automatically turns on GIT_DIFF_INCLUDE_UNTRACKED but
        /// it does not turn on GIT_DIFF_RECURSE_UNTRACKED_DIRS.  Add that
        /// flag if you want the content of every single UNTRACKED file.
        /// </summary>
        GIT_DIFF_SHOW_UNTRACKED_CONTENT = (1 << 25),

        /// <summary>
        /// When generating output, include the names of unmodified files if
        /// they are included in the git_diff.  Normally these are skipped in
        /// the formats that list files (e.g. name-only, name-status, raw).
        /// Even with this, these will not be included in patch format.
        /// </summary>
        GIT_DIFF_SHOW_UNMODIFIED = (1 << 26),

        /// <summary>
        /// Use the "patience diff" algorithm
        /// </summary>
        GIT_DIFF_PATIENCE = (1 << 28),

        /// <summary>
        /// Take extra time to find minimal diff
        /// </summary>
        GIT_DIFF_MINIMAL = (1 << 29),

        /// <summary>
        /// Include the necessary deflate / delta information so that `git-apply`
        /// can apply given diff information to binary files.
        /// </summary>
        GIT_DIFF_SHOW_BINARY = (1 << 30),
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int diff_notify_cb(
        IntPtr diff_so_far,
        IntPtr delta_to_add,
        IntPtr matched_pathspec,
        IntPtr payload);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int diff_progress_cb(
        IntPtr diff_so_far,
        IntPtr old_path,
        IntPtr new_path,
        IntPtr payload);

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffOptions : IDisposable
    {
        public uint Version = 1;
        public GitDiffOptionFlags Flags;

        /* options controlling which files are in the diff */

        public SubmoduleIgnore IgnoreSubmodules;
        public GitStrArrayManaged PathSpec;
        public diff_notify_cb NotifyCallback;
        public diff_progress_cb ProgressCallback;
        public IntPtr Payload;

        /* options controlling how to diff text is generated */

        public uint ContextLines;
        public uint InterhunkLines;
        public ushort IdAbbrev;
        public long MaxSize;
        public IntPtr OldPrefixString;
        public IntPtr NewPrefixString;

        public void Dispose()
        {
            PathSpec.Dispose();
        }
    }

    [Flags]
    internal enum GitDiffFlags
    {
        GIT_DIFF_FLAG_BINARY = (1 << 0),
        GIT_DIFF_FLAG_NOT_BINARY = (1 << 1),
        GIT_DIFF_FLAG_VALID_ID = (1 << 2),
        GIT_DIFF_FLAG_EXISTS = (1 << 3),
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_diff_file
    {
        public git_oid Id;
        public char* Path;
        public long Size;
        public GitDiffFlags Flags;
        public ushort Mode;
        public ushort IdAbbrev;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_diff_delta
    {
        public ChangeKind status;
        public GitDiffFlags flags;
        public ushort similarity;
        public ushort nfiles;
        public git_diff_file old_file;
        public git_diff_file new_file;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffHunk
    {
        public int OldStart;
        public int OldLines;
        public int NewStart;
        public int NewLines;
        public UIntPtr HeaderLen;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] Header;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffLine
    {
        public GitDiffLineOrigin lineOrigin;
        public int OldLineNo;
        public int NewLineNo;
        public int NumLines;
        public UIntPtr contentLen;
        public long contentOffset;
        public IntPtr content;
    }

    enum GitDiffLineOrigin : byte
    {
        GIT_DIFF_LINE_CONTEXT = 0x20, //' ',
        GIT_DIFF_LINE_ADDITION = 0x2B, //'+',
        GIT_DIFF_LINE_DELETION = 0x2D, //'-',
        GIT_DIFF_LINE_ADD_EOFNL = 0x0A, //'\n', /**< LF was added at end of file */
        GIT_DIFF_LINE_DEL_EOFNL = 0x0, //'\0', /**< LF was removed at end of file */

        /* these values will only be sent to a `git_diff_output_fn` */
        GIT_DIFF_LINE_FILE_HDR = 0x46, //'F',
        GIT_DIFF_LINE_HUNK_HDR = 0x48, //'H',
        GIT_DIFF_LINE_BINARY = 0x42, //'B',
    }

    enum GitDiffFormat
    {
        GIT_DIFF_FORMAT_PATCH = 1, // < full git diff
        GIT_DIFF_FORMAT_PATCH_HEADER = 2, // < just the file headers of patch
        GIT_DIFF_FORMAT_RAW = 3, // < like git diff --raw
        GIT_DIFF_FORMAT_NAME_ONLY = 4, // < like git diff --name-only
        GIT_DIFF_FORMAT_NAME_STATUS = 5, // < like git diff --name-status
    }

    [Flags]
    enum GitDiffFindFlags
    {
        // Obey `diff.renames`. Overridden by any other GIT_DIFF_FIND_... flag.
        GIT_DIFF_FIND_BY_CONFIG = 0,

        // Look for renames? (`--find-renames`)
        GIT_DIFF_FIND_RENAMES = (1 << 0),
        // consider old side of modified for renames? (`--break-rewrites=N`)
        GIT_DIFF_FIND_RENAMES_FROM_REWRITES = (1 << 1),

        // look for copies? (a la `--find-copies`)
        GIT_DIFF_FIND_COPIES = (1 << 2),
        // consider unmodified as copy sources? (`--find-copies-harder`)
        GIT_DIFF_FIND_COPIES_FROM_UNMODIFIED = (1 << 3),

        // mark large rewrites for split (`--break-rewrites=/M`)
        GIT_DIFF_FIND_REWRITES = (1 << 4),
        // actually split large rewrites into delete/add pairs
        GIT_DIFF_BREAK_REWRITES = (1 << 5),
        // mark rewrites for split and break into delete/add pairs
        GIT_DIFF_FIND_AND_BREAK_REWRITES =
            (GIT_DIFF_FIND_REWRITES | GIT_DIFF_BREAK_REWRITES),

        // find renames/copies for untracked items in working directory
        GIT_DIFF_FIND_FOR_UNTRACKED = (1 << 6),

        // turn on all finding features
        GIT_DIFF_FIND_ALL = (0x0ff),

        // measure similarity ignoring leading whitespace (default)
        GIT_DIFF_FIND_IGNORE_LEADING_WHITESPACE = 0,
        // measure similarity ignoring all whitespace
        GIT_DIFF_FIND_IGNORE_WHITESPACE = (1 << 12),
        // measure similarity including all data
        GIT_DIFF_FIND_DONT_IGNORE_WHITESPACE = (1 << 13),
        // measure similarity only by comparing SHAs (fast and cheap)
        GIT_DIFF_FIND_EXACT_MATCH_ONLY = (1 << 14),

        // do not break rewrites unless they contribute to a rename
        GIT_DIFF_BREAK_REWRITES_FOR_RENAMES_ONLY = (1 << 15),

        // Remove any UNMODIFIED deltas after find_similar is done.
        GIT_DIFF_FIND_REMOVE_UNMODIFIED = (1 << 16),
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffFindOptions
    {
        public uint Version = 1;
        public GitDiffFindFlags Flags;
        public ushort RenameThreshold;
        public ushort RenameFromRewriteThreshold;
        public ushort CopyThreshold;
        public ushort BreakRewriteThreshold;
        public UIntPtr RenameLimit;

        // TODO
        public IntPtr SimilarityMetric;
    }

    [Flags]
    enum GitDiffBinaryType
    {
        // There is no binary delta.
        GIT_DIFF_BINARY_NONE = 0,

        // The binary data is the literal contents of the file. */
        GIT_DIFF_BINARY_LITERAL,

        // The binary data is the delta from one side to the other. */
        GIT_DIFF_BINARY_DELTA,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffBinaryFile
    {
        public GitDiffBinaryType Type;
        public IntPtr Data;
        public UIntPtr DataLen;
        public UIntPtr InflatedLen;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffBinary
    {
        public uint ContainsData;
        public GitDiffBinaryFile OldFile;
        public GitDiffBinaryFile NewFile;
    }
}
