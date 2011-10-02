namespace LibGit2Sharp.Core
{
    internal class FilePath
    {
        private readonly string native;
        private readonly string posix;

        private FilePath(string path)
        {
            native = PosixPathHelper.ToNative(path);
            posix = PosixPathHelper.ToPosix(path);
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
            return path == null ? null : new FilePath(path);
        }
    }
}
