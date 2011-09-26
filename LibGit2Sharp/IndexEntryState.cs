namespace LibGit2Sharp
{
    public enum IndexEntryState
    {
        NotModified,
        NewUntracked,
        NewStaged,
        ModifiedUnstaged,
        ModifiedStaged,
        RemovedUnstaged,
        RemovedStaged,
    }
}
