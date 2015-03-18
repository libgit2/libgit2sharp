namespace LibGit2Sharp
{
    /// <summary>
    /// Provides optional additional information to the Repository to be opened.
    /// </summary>
    public sealed class RepositoryOptions
    {
        /// <summary>
        /// Overrides the probed location of the working directory of a standard repository,
        /// or, combined with <see cref="IndexPath"/>, would
        /// allow to work against a bare repository as it was a standard one.
        /// <para>
        ///   The path has to lead to an existing directory.
        /// </para>
        /// </summary>
        public string WorkingDirectoryPath { get; set; }

        /// <summary>
        /// Overrides the probed location of the Index file of a standard repository,
        /// or, combined with <see cref="WorkingDirectoryPath"/>, would
        /// allow to work against a bare repository as it was a standard one.
        /// <para>
        ///   The path has either to lead to an existing valid Index file,
        ///   or to a non existent Index file which will be eventually created.
        /// </para>
        /// </summary>
        public string IndexPath { get; set; }

        /// <summary>
        /// Overrides the probed location of the Global configuration file of a repository.
        /// <para>
        ///   The path has either to lead to an existing valid configuration file,
        ///   or to a non existent configuration file which will be eventually created.
        /// </para>
        /// </summary>
        public string GlobalConfigurationLocation { get; set; }

        /// <summary>
        /// Overrides the probed location of the XDG configuration file of a repository.
        /// <para>
        ///   The path has either to lead to an existing valid configuration file,
        ///   or to a non existent configuration file which will be eventually created.
        /// </para>
        /// </summary>
        public string XdgConfigurationLocation { get; set; }

        /// <summary>
        /// Overrides the probed location of the System configuration file of a repository.
        /// <para>
        ///   The path has to lead to an existing valid configuration file,
        ///   or to a non existent configuration file which will be eventually created.
        /// </para>
        /// </summary>
        public string SystemConfigurationLocation { get; set; }

        /// <summary>
        /// Overrides the default identity to be used when creating reflog entries.
        /// <para>
        ///   When unset the identity will be retreived from the repository's configuration.
        ///   When no identity can be found in the repository configuration stores, a fake
        ///   identity ("unknown" as both name and email), will be used.
        /// </para>
        /// </summary>
        public Identity Identity { get; set; }
    }
}
