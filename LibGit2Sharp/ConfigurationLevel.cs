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
        Local = 4,

        /// <summary>
        ///   The global ~/.gitconfig of the current user.
        /// </summary>
        Global = 3,

        /// <summary>
        /// The global ~/.config/git/config of the current user
        /// </summary>
        XDG = 2,

        /// <summary>
        ///   The system wide .gitconfig.
        /// </summary>
        System = 1,
    }
}
