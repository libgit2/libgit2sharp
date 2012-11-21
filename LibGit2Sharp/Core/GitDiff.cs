using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [Flags]
    internal enum GitDiffOptionFlags
    {
        /** Normal diff, the default */
        GIT_DIFF_NORMAL = 0,
        /** Reverse the sides of the diff */
        GIT_DIFF_REVERSE = (1 << 0),
        /** Treat all files as text, disabling binary attributes & detection */
        GIT_DIFF_FORCE_TEXT = (1 << 1),
        /** Ignore all whitespace */
        GIT_DIFF_IGNORE_WHITESPACE = (1 << 2),
        /** Ignore changes in amount of whitespace */
        GIT_DIFF_IGNORE_WHITESPACE_CHANGE = (1 << 3),
        /** Ignore whitespace at end of line */
        GIT_DIFF_IGNORE_WHITESPACE_EOL = (1 << 4),
        /** Exclude submodules from the diff completely */
        GIT_DIFF_IGNORE_SUBMODULES = (1 << 5),
        /** Use the "patience diff" algorithm (currently unimplemented) */
        GIT_DIFF_PATIENCE = (1 << 6),
        /** Include ignored files in the diff list */
        GIT_DIFF_INCLUDE_IGNORED = (1 << 7),
        /** Include untracked files in the diff list */
        GIT_DIFF_INCLUDE_UNTRACKED = (1 << 8),
        /** Include unmodified files in the diff list */
        GIT_DIFF_INCLUDE_UNMODIFIED = (1 << 9),
        /** Even with the GIT_DIFF_INCLUDE_UNTRACKED flag, when an untracked
         *  directory is found, only a single entry for the directory is added
         *  to the diff list; with this flag, all files under the directory will
         *  be included, too.
         */
        GIT_DIFF_RECURSE_UNTRACKED_DIRS = (1 << 10),
        /** If the pathspec is set in the diff options, this flags means to
         *  apply it as an exact match instead of as an fnmatch pattern.
         */
        GIT_DIFF_DISABLE_PATHSPEC_MATCH = (1 << 11),
        /** Use case insensitive filename comparisons */
        GIT_DIFF_DELTAS_ARE_ICASE = (1 << 12),
        /** When generating patch text, include the content of untracked files */
        GIT_DIFF_INCLUDE_UNTRACKED_CONTENT = (1 << 13),
        /** Disable updating of the `binary` flag in delta records.  This is
         *  useful when iterating over a diff if you don't need hunk and data
         *  callbacks and want to avoid having to load file completely.
         */
        GIT_DIFF_SKIP_BINARY_CHECK = (1 << 14),
        /** Normally, a type change between files will be converted into a
         *  DELETED record for the old and an ADDED record for the new; this
         *  options enabled the generation of TYPECHANGE delta records.
         */
        GIT_DIFF_INCLUDE_TYPECHANGE = (1 << 15),
        /** Even with GIT_DIFF_INCLUDE_TYPECHANGE, blob->tree changes still
         *  generally show as a DELETED blob.  This flag tries to correctly
         *  label blob->tree transitions as TYPECHANGE records with new_file's
         *  mode set to tree.  Note: the tree SHA will not be available.
         */
        GIT_DIFF_INCLUDE_TYPECHANGE_TREES = (1 << 16),
        /** Ignore file mode changes */
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
        public GitDiffOptionFlags Flags;
        public ushort ContextLines;
        public ushort InterhunkLines;

        // NB: These are char*s to UTF8 strings, finna marshal them by hand
        public IntPtr OldPrefixString;
        public IntPtr NewPrefixString;

        public GitStrArrayIn PathSpec;
        public ulong MaxSize;

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
        public long Size;
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
