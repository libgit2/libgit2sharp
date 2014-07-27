namespace LibGit2Sharp.Core
{
    internal static class GitDiffExtensions
    {
        public static bool IsBinary(this GitDiffDelta delta)
        {
            return delta.Flags.HasFlag(GitDiffFlags.GIT_DIFF_FLAG_BINARY);
        }
    }
}
