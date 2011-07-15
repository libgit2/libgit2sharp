using System.IO;

namespace LibGit2Sharp.Core
{
    internal static class PosixPathHelper
    {
        private const char posixDirectorySeparatorChar = '/';

        public static string ToPosix(string nativePath)
        {
            if (posixDirectorySeparatorChar == Path.DirectorySeparatorChar)
            {
                return nativePath;
            }

            if (nativePath == null)
            {
                return null;
            }

            return nativePath.Replace(Path.DirectorySeparatorChar, posixDirectorySeparatorChar);
        }

        public static string ToNative(string posixPath)
        {
            if (posixDirectorySeparatorChar == Path.DirectorySeparatorChar)
            {
                return posixPath;
            }

            if (posixPath == null)
            {
                return null;
            }

            return posixPath.Replace(posixDirectorySeparatorChar, Path.DirectorySeparatorChar); ;
        }
    }
}