using System.IO;

namespace LibGit2Sharp.Core
{
    internal class FilePath
    {
        internal static FilePath Empty = new FilePath(string.Empty);

        private const char posixDirectorySeparatorChar = '/';

        private readonly string native;
        private readonly string posix;

        private FilePath(string path)
        {
            native = Replace(path, posixDirectorySeparatorChar, Path.DirectorySeparatorChar);
            posix = Replace(path, Path.DirectorySeparatorChar, posixDirectorySeparatorChar);
        }

        public string Native
        {
            get { return native; }
        }

        public string Posix
        {
            get { return posix; }
        }

        public override string ToString()
        {
            return Native;
        }

        public static implicit operator FilePath(string path)
        {
            switch (path)
            {
                case null:
                    return null;

                case "":
                    return Empty;

                default:
                    return new FilePath(path);
            }
        }

        private static string Replace(string path, char oldChar, char newChar)
        {
            if (oldChar == newChar)
            {
                return path;
            }

            return path == null ? null : path.Replace(oldChar, newChar);
        }
    }
}
