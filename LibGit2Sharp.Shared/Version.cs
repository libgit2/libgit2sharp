using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Gets the current LibGit2Sharp version.
    /// </summary>
    public class Version
    {
        private readonly Assembly assembly = typeof(Repository).Assembly;

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
                var attribute = (AssemblyInformationalVersionAttribute)assembly
                   .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                   .Single();

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
        public virtual string LibGit2CommitSha
        {
            get { return RetrieveAbbrevShaFrom("libgit2_hash.txt"); }
        }

        /// <summary>
        /// Returns the SHA hash for the LibGit2Sharp library.
        /// </summary>
        public virtual string LibGit2SharpCommitSha
        {
            get { return RetrieveAbbrevShaFrom("libgit2sharp_hash.txt"); }
        }

        private string RetrieveAbbrevShaFrom(string name)
        {
            string sha = ReadContentFromResource(assembly, name) ?? "unknown";

            return sha.Substring(0, 7);
        }

        /// <summary>
        /// Returns a string representing the LibGit2Sharp version.
        /// </summary>
        /// <para>
        ///   The format of the version number is as follows:
        ///   <para>Major.Minor.Patch-LibGit2Sharp_abbrev_hash-libgit2_abbrev_hash (x86|amd64 - features)</para>
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
                                 "{0}-{1}-{2} ({3} - {4})",
                                 InformationalVersion,
                                 LibGit2SharpCommitSha,
                                 LibGit2CommitSha,
                                 Platform.ProcessorArchitecture,
                                 features);
        }

        private string ReadContentFromResource(Assembly assembly, string partialResourceName)
        {
            string name = string.Format(CultureInfo.InvariantCulture, "LibGit2Sharp.{0}", partialResourceName);
            using (var sr = new StreamReader(assembly.GetManifestResourceStream(name)))
            {
                return sr.ReadLine();
            }
        }
    }
}
