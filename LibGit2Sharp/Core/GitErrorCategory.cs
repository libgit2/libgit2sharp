namespace LibGit2Sharp.Core
{
    internal enum GitErrorCategory
    {
        Unknown = -1,
        NoMemory,
        Os,
        Invalid,
        Reference,
        Zlib,
        Repository,
        Config,
        Regex,
        Odb,
        Index,
        Object,
        Net,
        Tag,
        Tree,
        Indexer,
        Ssl,
        Submodule,
        Thread,
        Stash,
        Checkout,
    }
}
