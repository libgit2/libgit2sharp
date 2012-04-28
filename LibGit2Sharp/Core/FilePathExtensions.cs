namespace LibGit2Sharp.Core
{
    internal static class FilePathExtensions
    {
        internal static FilePath Combine(this FilePath @this, string childPath)
        {
            return @this.IsNullOrEmpty() ? childPath : @this.Posix + "/" + childPath;
        }

        internal static bool IsNullOrEmpty(this FilePath @this)
        {
            return ReferenceEquals(@this, null) || @this.Posix.Length == 0;
        }
    }
}