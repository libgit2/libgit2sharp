﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Gets the current LibGit2Sharp version.
    /// </summary>
    public class Version
    {
        private Assembly assembly = typeof(Repository).Assembly;

        /// <summary>
        /// Returns the <see cref="System.Version" /> of the 
        /// the LibGit2Sharp library.
        /// </summary>
        public System.Version MajorMinorPatch
        {
            get
            {
                return assembly.GetName().Version;
            }
        }

        /// <summary>
        /// Returns all the optional features that were compiled into
        /// libgit2.
        /// </summary>
        /// <returns>A <see cref="BuiltInFeatures"/> enumeration.</returns>
        public BuiltInFeatures Features()
        {
            return Proxy.git_libgit2_features();
        }

        /// <summary>
        /// Returns the SHA hash for the libgit2 library. 
        /// </summary>
        public string LibGit2CommitSha
        {
            get
            {
                return ReadContentFromResource(assembly, "libgit2_hash.txt").Substring(0, 7);
            }
        }

        /// <summary>
        /// Returns the SHA hash for the LibGit2Sharp library.
        /// </summary>
        public string LibGit2SharpCommitSha
        {
            get
            {
                return ReadContentFromResource(assembly, "libgit2sharp_hash.txt").Substring(0, 7);
            }
        }

        /// <summary>
        /// Determines whether the current process is a 64-bit process.
        /// </summary>
        public bool Is64BitProcess
        {
            get
            {
                return Environment.Is64BitProcess;
            }
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
            string features = Features().ToString();

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}-{2} ({3} - {4})",
                MajorMinorPatch.ToString(3),
                LibGit2SharpCommitSha,
                LibGit2CommitSha,
                NativeMethods.ProcessorArchitecture,
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
