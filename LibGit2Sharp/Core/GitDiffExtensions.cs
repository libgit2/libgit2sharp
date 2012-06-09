namespace LibGit2Sharp.Core
{
    internal static class GitDiffExtensions
    {
        public static bool IsBinary(this GitDiffDelta delta)
        {
            //TODO Fix the interop issue on amd64 and use GitDiffDelta.Binary
            return delta.OldFile.Flags.Has(GitDiffFileFlags.GIT_DIFF_FILE_BINARY) || delta.NewFile.Flags.Has(GitDiffFileFlags.GIT_DIFF_FILE_BINARY);
        }
    }
}
