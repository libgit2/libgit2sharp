namespace LibGit2Sharp
{
    /// <summary>
    /// Specifies the level of configuration to use.
    /// </summary>
    public enum ConfigurationLevel
    {
        /// <summary>
        /// The local .git/config of the current repository.
        /// </summary>
        Local = 5,

        /// <summary>
        /// The global ~/.gitconfig of the current user.
        /// </summary>
        Global = 4,

        /// <summary>
        /// The global ~/.config/git/config of the current user.
        /// </summary>
        Xdg = 3,

        /// <summary>
        /// The system wide .gitconfig.
        /// </summary>
        System = 2,

        /// <summary>
        /// Another system-wide configuration on Windows.
        /// </summary>
        ProgramData = 1,
    }
}
