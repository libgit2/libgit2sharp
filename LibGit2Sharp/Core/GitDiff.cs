using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum GitDiffOptionFlags
    {
        GIT_DIFF_NORMAL = 0,
        GIT_DIFF_REVERSE = (1 << 0),
        GIT_DIFF_FORCE_TEXT = (1 << 1),
        GIT_DIFF_IGNORE_WHITESPACE = (1 << 2),
        GIT_DIFF_IGNORE_WHITESPACE_CHANGE = (1 << 3),
        GIT_DIFF_IGNORE_WHITESPACE_EOL = (1 << 4),
        GIT_DIFF_IGNORE_SUBMODULES = (1 << 5),
        GIT_DIFF_PATIENCE = (1 << 6),
        GIT_DIFF_INCLUDE_IGNORED = (1 << 7),
        GIT_DIFF_INCLUDE_UNTRACKED = (1 << 8),
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffOptions
    {
        public GitDiffOptionFlags Flags;
        public ushort ContextLines;
        public ushort InterhunkLines;

        // NB: These are char*s to UTF8 strings, finna marshal them by hand
        public IntPtr OldPrefixString;
        public IntPtr NewPrefixString;

        public UnSafeNativeMethods.git_strarray PathSpec;
    }

    [Flags]
    internal enum GitDiffFileFlags
    {
        GIT_DIFF_FILE_VALID_OID = (1 << 0),
        GIT_DIFF_FILE_FREE_PATH = (1 << 1),
        GIT_DIFF_FILE_BINARY = (1 << 2),
        GIT_DIFF_FILE_NOT_BINARY = (1 << 3),
        GIT_DIFF_FILE_FREE_DATA = (1 << 4),
        GIT_DIFF_FILE_UNMAP_DATA = (1 << 5),
    }

    public enum GitDeltaType
    {
        GIT_DELTA_UNMODIFIED = 0,
        GIT_DELTA_ADDED = 1,
        GIT_DELTA_DELETED = 2,
        GIT_DELTA_MODIFIED = 3,
        GIT_DELTA_RENAMED = 4,
        GIT_DELTA_COPIED = 5,
        GIT_DELTA_IGNORED = 6,
        GIT_DELTA_UNTRACKED = 7,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffFile
    {
        public GitOid Oid;
        public IntPtr Path;
        public ushort Mode;
        public long Size;
        public GitDiffFileFlags Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffDelta
    {
        public GitDiffFile OldFile;
        public GitDiffFile NewFile;
        public GitDeltaType Status;
        public UIntPtr Similarity;
        public IntPtr Binary;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffRange
    {
        public IntPtr OldStart;
        public IntPtr OldLines;
        public IntPtr NewStart;
        public IntPtr NewLines;
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
}