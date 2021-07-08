using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class ContentChangeLine
    {
        public int OldLineNo;
        public int NewLineNo;
        public int NumLines;

        internal ContentChangeLine(GitDiffLine line)
        {
            OldLineNo = line.OldLineNo;
            NewLineNo = line.NewLineNo;
            NumLines = line.NumLines;
        }
    }
}
