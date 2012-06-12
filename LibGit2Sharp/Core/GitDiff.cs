using System;
using System.Runtime.InteropServices;

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
