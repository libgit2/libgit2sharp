namespace LibGit2Sharp
{
    /// <summary>
    /// Optional parameters when invoking Init.
    /// </summary>
    public sealed class InitOptions
    {
        /// <summary>
        /// Use the specified name for the initial branch in the newly created repository.
        /// If not specified, fall back to the default name
        /// </summary>
        public string InitialHead { get; set; }
    }
}
