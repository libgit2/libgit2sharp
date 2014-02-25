using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public sealed class RevertOptions
    {
        public int Mainline;

        internal GitRevertOpts ToNative()
        {
            return new GitRevertOpts
            {
                Mainline = (uint)Mainline
            };
        }
    }
}
