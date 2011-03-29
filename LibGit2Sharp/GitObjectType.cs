namespace LibGit2Sharp
{
    public enum GitObjectType
    {
        Any = -2,
        Bad = -1,
        Ext1 = 0,
        Commit = 1,
        Tree = 2,
        Blob = 3,
        Tag = 4,
        Ext2 = 5,
        OfsDelta = 6,
        RefDelta = 7
    }
}