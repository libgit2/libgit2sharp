using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitNoteData 
    {
        public GitOid BlobOid;
        public GitOid TargetOid;
    }
}
