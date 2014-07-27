using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Global settings for libgit2 and LibGit2Sharp.
    /// </summary>
    public static class GlobalSettings
    {
        /// <summary>
        /// Returns all the optional features that were compiled into
        /// libgit2.
        /// </summary>
        /// <returns>A <see cref="BuiltInFeatures"/> enumeration.</returns>
        public static BuiltInFeatures Features()
        {
            return Proxy.git_libgit2_features();
        }
    }
}
