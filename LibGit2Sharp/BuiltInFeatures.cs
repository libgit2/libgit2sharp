using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Flags to identify libgit2 compiled features.
    /// </summary>
    [Flags]
    public enum BuiltInFeatures
    {
        /// <summary>
        /// No optional features are compiled into libgit2.
        /// </summary>
        None = 0,

        /// <summary>
        /// Threading support is compiled into libgit2.
        /// </summary>
        Threads = (1 << 0),

        /// <summary>
        /// Support for remotes over the HTTPS protocol is compiled into
        /// libgit2.
        /// </summary>
        Https = (1 << 1),

        /// <summary>
        /// Support for remotes over the SSH protocol is compiled into
        /// libgit2.
        /// </summary>
        Ssh = (1 << 2),
    }
}
