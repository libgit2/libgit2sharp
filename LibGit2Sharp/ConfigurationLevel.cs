namespace LibGit2Sharp
{
    /// <summary>
    ///   Specifies the level of configuration to use.
    /// </summary>
    public enum ConfigurationLevel
    {
        /// <summary>
        ///   The local .git/config of the current repository.
        /// </summary>
        Local,

        /// <summary>
        ///   The global ~/.gitconfig of the current user.
        /// </summary>
        Global,

        /// <summary>
        ///   The system wide .gitconfig.
        /// </summary>
        System
    }
}