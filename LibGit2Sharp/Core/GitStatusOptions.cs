using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitStatusOptions : IDisposable
    {
        public uint Version = 1;

        public GitStatusShow Show;
        public GitStatusOptionFlags Flags;

        public GitStrArrayManaged PathSpec;

        public void Dispose()
        {
            PathSpec.Dispose();
        }
    }

    internal enum GitStatusShow
    {
        IndexAndWorkDir = 0,
        IndexOnly = 1,
        WorkDirOnly = 2,
    }

    [Flags]
    internal enum GitStatusOptionFlags
    {
        IncludeUntracked = (1 << 0),
        IncludeIgnored = (1 << 1),
        IncludeUnmodified = (1 << 2),
        ExcludeSubmodules = (1 << 3),
        RecurseUntrackedDirs = (1 << 4),
        DisablePathspecMatch = (1 << 5),
        RecurseIgnoredDirs = (1 << 6),
        RenamesHeadToIndex = (1 << 7),
        RenamesIndexToWorkDir = (1 << 8),
        SortCaseSensitively = (1 << 9),
        SortCaseInsensitively = (1 << 10),
        RenamesFromRewrites = (1 << 11),
        NoRefresh = (1 << 12),
        UpdateIndex = (1 << 13),
        IncludeUnreadable = (1 << 14),
        IncludeUnreadableAsUntracked = (1 << 15),
    }
}
