namespace LibGit2Sharp.Core
{
    internal unsafe struct git_push_update
    {
        public char* src_refname;
        public char* dst_refname;
        public GitOid src;
        public GitOid dst;
    }
}
