using System.Globalization;
using System.Reflection;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Gets the current LibGit2Sharp version.
    /// </summary>
    public class Version
    {
        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Version()
        { }

        internal static Version Build()
        {
            return new Version();
        }

        /// <summary>
        /// Returns version of the LibGit2Sharp library.
        /// </summary>
        public virtual string InformationalVersion
        {
            get
            {
                var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                return attribute.InformationalVersion;
            }
        }

        /// <summary>
        /// Returns all the optional features that were compiled into
        /// libgit2.
        /// </summary>
        /// <returns>A <see cref="BuiltInFeatures"/> enumeration.</returns>
        public virtual BuiltInFeatures Features
        {
            get { return Proxy.git_libgit2_features(); }
        }

        /// <summary>
        /// Returns the SHA hash for the libgit2 library.
        /// </summary>
        public virtual string LibGit2CommitSha => RetrieveAbbrevShaFrom(AssemblyCommitIds.LibGit2CommitSha);

        /// <summary>
        /// Returns the SHA hash for the LibGit2Sharp library.
        /// </summary>
        public virtual string LibGit2SharpCommitSha => RetrieveAbbrevShaFrom(AssemblyCommitIds.LibGit2SharpCommitSha);

        private string RetrieveAbbrevShaFrom(string sha)
        {
            var index = sha.Length > 7 ? 7 : sha.Length;
            return sha.Substring(0, index);
        }

        /// <summary>
        /// Returns a string representing the LibGit2Sharp version.
        /// </summary>
        /// <para>
        ///   The format of the version number is as follows:
        ///   <para>Major.Minor.Patch[-previewTag]+libgit2-{libgit2_abbrev_hash}.{LibGit2Sharp_hash} (arch - features)</para>
        /// </para>
        /// <returns></returns>
        public override string ToString()
        {
            return RetrieveVersion();
        }

        private string RetrieveVersion()
        {
            string features = Features.ToString();

            return string.Format(CultureInfo.InvariantCulture,
                                 "{0} ({1} - {2})",
                                 InformationalVersion,
                                 Platform.ProcessorArchitecture,
                                 features);
        }
    }
}
