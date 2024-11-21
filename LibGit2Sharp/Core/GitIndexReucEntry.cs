namespace LibGit2Sharp.Core
{
    internal unsafe struct git_index_reuc_entry
    {
        public uint AncestorMode;
        public uint OurMode;
        public uint TheirMode;
        public GitOid AncestorId;
        public GitOid OurId;
        public GitOid TheirId;
        public char* Path;
    }
}
