using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    public enum ExistingFileAction
    {
        OverwriteExisting = 0,
        SkipExisting = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    public class GitCheckoutOptions
    {
        public ExistingFileAction ExistingFileAction;
        [MarshalAs(UnmanagedType.SysInt)]
        public bool DisableFilters;
        public int DirMode;
        public int FileMode;
        public int FileOpenFlags;
    }
}