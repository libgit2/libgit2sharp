namespace LibGit2Sharp
{
    /// <summary>
    /// Information on a particular step of a rebase operation.
    /// </summary>
    public class RebaseStepInfo
    {
        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected RebaseStepInfo()
        { }

        internal RebaseStepInfo(RebaseStepOperation type, Commit commit, string exec)
        {
            Type = type;
            Commit = commit;
            Exec = exec;
        }

        /// <summary>
        /// The rebase operation type.
        /// </summary>
        public virtual RebaseStepOperation Type { get; private set; }

        /// <summary>
        /// The object ID the step is operating on.
        /// </summary>
        public virtual Commit Commit { get; private set; }

        /// <summary>
        /// Command to execute, if any.
        /// </summary>
        internal virtual string Exec { get; private set; }
    }
}
