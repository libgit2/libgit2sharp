namespace LibGit2Sharp
{
    /// <summary>
    /// Available logging levels.  When logging is enabled at a particular
    /// level, callers will be provided logging at the given level and all
    /// lower levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// No logging will be provided.
        /// </summary>
        None = 0,

        /// <summary>
        /// Severe errors that may impact the program's execution.
        /// </summary>
        Fatal = 1,

        /// <summary>
        /// Errors that do not impact the program's execution.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Warnings that suggest abnormal data.
        /// </summary>
        Warning = 3,

        /// <summary>
        /// Informational messages about program execution.
        /// </summary>
        Info = 4,

        /// <summary>
        /// Detailed data that allows for debugging.
        /// </summary>
        Debug = 5,

        /// <summary>
        /// Tracing is exceptionally detailed debugging data.
        /// </summary>
        Trace = 6,
    }
}
