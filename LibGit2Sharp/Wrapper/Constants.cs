namespace LibGit2Sharp.Wrapper
{
    public static class Constants
    {
        public static char DirectorySeparatorChar = '/';

        public const int GIT_ERROR = -1;
        public const int GIT_OID_RAWSZ = 20;
        public const int GIT_OID_HEXSZ = GIT_OID_RAWSZ * 2;

        public static string GIT_HEAD_FILE = "HEAD";
    }
}