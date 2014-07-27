namespace LibGit2Sharp
{
    /// <summary>
    /// Holds a summary of a change to a single file.
    /// </summary>
    public class ContentChangeStats
    {
        /// <summary>
        /// The number of lines added in the diff.
        /// </summary>
        public virtual int LinesAdded { get; private set; }

        /// <summary>
        /// The number of lines deleted in the diff.
        /// </summary>
        public virtual int LinesDeleted { get; private set; }

        /// <summary>
        /// For mocking.
        /// </summary>
        protected ContentChangeStats()
        { }

        internal ContentChangeStats(int added, int deleted)
        {
            LinesAdded = added;
            LinesDeleted = deleted;
        }
    }
}
