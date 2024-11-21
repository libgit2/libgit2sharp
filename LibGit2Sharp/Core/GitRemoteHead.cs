namespace LibGit2Sharp.Core
{
    internal unsafe struct git_remote_head
    {
        public int Local;
        public GitOid Oid;
        public GitOid Loid;
        public char* Name;
        public char* SymrefTarget;
    }
}
