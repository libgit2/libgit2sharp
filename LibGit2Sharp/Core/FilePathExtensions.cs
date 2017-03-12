namespace LibGit2Sharp.Core
{
    internal static class FilePathExtensions
    {
        internal static FilePath Combine(this FilePath filePath, string childPath)
        {
            return filePath.IsNullOrEmpty() ? childPath : filePath.Posix + "/" + childPath;
        }

        internal static bool IsNullOrEmpty(this FilePath filePath)
        {
            return ReferenceEquals(filePath, null) || filePath.Posix.Length == 0;
        }
    }
}
