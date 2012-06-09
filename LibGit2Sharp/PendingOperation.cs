namespace LibGit2Sharp
{
    public enum PendingOperation
    {
        None = 0,
        RebaseInteractive = 1,
        RebaseMerge = 2,
        Rebase = 3,
        ApplyMailbox = 4,
        ApplyMailboxOrRebase = 5,
        Merge = 6,
        CherryPick = 7,
        Bisect = 8,
    }
}
