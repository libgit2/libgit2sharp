using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitError
    {
        public IntPtr Message;
        public IntPtr Klass;
    }

    internal enum GitErrorClass
    {
        GITERR_NOMEMORY,
        GITERR_OS,
        GITERR_INVALID,
        GITERR_REFERENCE,
        GITERR_ZLIB,
        GITERR_REPOSITORY,
        GITERR_CONFIG,
        GITERR_REGEX,
        GITERR_ODB,
        GITERR_INDEX,
        GITERR_OBJECT,
        GITERR_NET,
        GITERR_TAG,
        GITERR_TREE,
    }
}
