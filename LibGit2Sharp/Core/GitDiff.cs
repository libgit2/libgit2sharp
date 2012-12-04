using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum GitDiffOptionFlags
    {
        /// <summary>
        ///   Normal diff, the default.
        /// </summary>
        GIT_DIFF_NORMAL = 0,

        /// <summary>
        ///   Reverse the sides of the diff.
        /// </summary>
        GIT_DIFF_REVERSE = (1 << 0),

        /// <summary>
        ///   Treat all files as text, disabling binary attributes and detection.
        /// </summary>
        GIT_DIFF_FORCE_TEXT = (1 << 1),

        /// <summary>
        ///   Ignore all whitespace.
        /// </summary>
        GIT_DIFF_IGNORE_WHITESPACE = (1 << 2),

        /// <summary>
        ///   Ignore changes in amount of whitespace.
        /// </summary>
        GIT_DIFF_IGNORE_WHITESPACE_CHANGE = (1 << 3),

        /// <summary>
        ///   Ignore whitespace at end of line.
        /// </summary>
        GIT_DIFF_IGNORE_WHITESPACE_EOL = (1 << 4),

        /// <summary>
        ///   Exclude submodules from the diff completely.
        /// </summary>
        GIT_DIFF_IGNORE_SUBMODULES = (1 << 5),

        /// <summary>
        ///   Use the "patience diff" algorithm (currently unimplemented).
        /// </summary>
        GIT_DIFF_PATIENCE = (1 << 6),

        /// <summary>
        ///   Include ignored files in the diff list.
        /// </summary>
        GIT_DIFF_INCLUDE_IGNORED = (1 << 7),

        /// <summary>
        ///   Include untracked files in the diff list.
        /// </summary>
        GIT_DIFF_INCLUDE_UNTRACKED = (1 << 8),

        /// <summary>
        ///   Include unmodified files in the diff list.
        /// </summary>
        GIT_DIFF_INCLUDE_UNMODIFIED = (1 << 9),

        /// <summary>
        ///   Even with the GIT_DIFF_INCLUDE_UNTRACKED flag, when an untracked
        ///   directory is found, only a single entry for the directory is added
        ///   to the diff list; with this flag, all files under the directory will
        ///   be included, too.
        /// </summary>
        GIT_DIFF_RECURSE_UNTRACKED_DIRS = (1 << 10),

        /// <summary>
        ///  If the pathspec is set in the diff options, this flags means to
        ///  apply it as an exact match instead of as an fnmatch pattern.
        /// </summary>
        GIT_DIFF_DISABLE_PATHSPEC_MATCH = (1 << 11),

        /// <summary>
        ///   Use case insensitive filename comparisons.
        /// </summary>
        GIT_DIFF_DELTAS_ARE_ICASE = (1 << 12),

        /// <summary>
        ///   When generating patch text, include the content of untracked files.
        /// </summary>
        GIT_DIFF_INCLUDE_UNTRACKED_CONTENT = (1 << 13),

        /// <summary>
        ///  Disable updating of the `binary` flag in delta records.  This is
        ///  useful when iterating over a diff if you don't need hunk and data
        ///  callbacks and want to avoid having to load file completely.
        /// </summary>
        GIT_DIFF_SKIP_BINARY_CHECK = (1 << 14),

        /// <summary>
        /// Normally, a type change between files will be converted into a
        /// DELETED record for the old and an ADDED record for the new; this
        /// options enabled the generation of TYPECHANGE delta records.
        /// </summary>
        GIT_DIFF_INCLUDE_TYPECHANGE = (1 << 15),

        /// <summary>
        ///  Even with GIT_DIFF_INCLUDE_TYPECHANGE, blob->tree changes still
        ///  generally show as a DELETED blob.  This flag tries to correctly
        ///  label blob->tree transitions as TYPECHANGE records with new_file's
        ///  mode set to tree.  Note: the tree SHA will not be available.
        /// </summary>
        GIT_DIFF_INCLUDE_TYPECHANGE_TREES = (1 << 16),

        /// <summary>
        ///   Ignore file mode changes.
        /// </summary>
        GIT_DIFF_IGNORE_FILEMODE = (1 << 17),
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitStrArrayIn : IDisposable
    {
        public IntPtr strings;
        public uint size;

        public static GitStrArrayIn BuildFrom(FilePath[] paths)
        {
            var nbOfPaths = paths.Length;
            var pathPtrs = new IntPtr[nbOfPaths];

            for (int i = 0; i < nbOfPaths; i++)
            {
                var s = paths[i];
                pathPtrs[i] = FilePathMarshaler.FromManaged(s);
            }

            int dim = IntPtr.Size * nbOfPaths;

            IntPtr arrayPtr = Marshal.AllocHGlobal(dim);
            Marshal.Copy(pathPtrs, 0, arrayPtr, nbOfPaths);

            return new GitStrArrayIn { strings = arrayPtr, size = (uint)nbOfPaths };
        }

        public void Dispose()
        {
            if (size == 0)
            {
                return;
            }

            var nbOfPaths = (int)size;

            var pathPtrs = new IntPtr[nbOfPaths];
            Marshal.Copy(strings, pathPtrs, 0, nbOfPaths);

            for (int i = 0; i < nbOfPaths; i ++)
            {
                Marshal.FreeHGlobal(pathPtrs[i]);
            }

            Marshal.FreeHGlobal(strings);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffOptions : IDisposable
    {
        public uint Version = 1;
        public GitDiffOptionFlags Flags;
        public ushort ContextLines;
        public ushort InterhunkLines;

        // NB: These are char*s to UTF8 strings, finna marshal them by hand
        public IntPtr OldPrefixString;
        public IntPtr NewPrefixString;

        public GitStrArrayIn PathSpec;
        public Int64 MaxSize;

        public void Dispose()
        {
            if (PathSpec == null)
            {
                return;
            }

            PathSpec.Dispose();

            PathSpec.size = 0;
        }
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

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffFile
    {
        public GitOid Oid;
        public IntPtr Path;
        public Int64 Size;
        public GitDiffFileFlags Flags;
        public ushort Mode;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffDelta
    {
        public GitDiffFile OldFile;
        public GitDiffFile NewFile;
        public ChangeKind Status;
        public uint Similarity;
        public int Binary;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class GitDiffRange
    {
        public int OldStart;
        public int OldLines;
        public int NewStart;
        public int NewLines;
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
