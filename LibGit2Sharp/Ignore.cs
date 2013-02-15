﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides methods to manage the rules ensuring that some specific
    ///   untracked files are ignored.
    /// </summary>
    public class Ignore
    {
        private readonly Repository repo;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Ignore()
        { }

        internal Ignore(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Adds a custom .gitignore rule that will be applied to futher operations to the Index. This is in addition
        ///   to the standard .gitignore rules that would apply as a result of the system/user/repo .gitignore
        /// </summary>
        /// <param name="rules">The content of a .gitignore file that will be applied.</param>
        public virtual void AddTemporaryRules(IEnumerable<string> rules)
        {
            var allRules = rules.Aggregate(new StringBuilder(), (acc, x) =>
            {
                acc.Append(x);
                acc.Append("\n");
                return acc;
            });

            Proxy.git_ignore_add_rule(repo.Handle, allRules.ToString());
        }

        /// <summary>
        ///   Write permanent path ignore rules to the .gitignore file for the repository.
        /// </summary>
        /// <param name="rules">The rules to be written to the repository's .gitignore file.</param>
        public virtual void AddPermanentRules(IEnumerable<string> rules)
        {
            string gitIgnorePath = repo.Info.Path+".."+Path.DirectorySeparatorChar+".gitignore";
            StreamWriter gitIgnoreFile = new StreamWriter(gitIgnorePath, true);

            var allRules = rules.Aggregate(new StringBuilder(), (acc, x) =>
            {
                //# .gitignore wants forward-slashes
                acc.Append(x.Replace("\\", "/"));
                acc.AppendLine();
                return acc;
            });

            gitIgnoreFile.Write(allRules.ToString());
            gitIgnoreFile.Close();

            Proxy.git_ignore_add_rule(repo.Handle, allRules.ToString());
        }

        /// <summary>
        ///   Resets all custom rules that were applied via calls to
        ///   <see cref="AddTemporaryRules"/> - note that this will not affect
        ///   the application of the user/repo .gitignores.
        /// </summary>
        public virtual void ResetAllTemporaryRules()
        {
            Proxy.git_ignore_clear_internal_rules(repo.Handle);
        }

        /// <summary>
        ///    Given a relative path, this method determines whether a path should be ignored, checking
        ///    both the custom ignore rules as well as the "normal" .gitignores.
        /// </summary>
        /// <param name="relativePath">A path relative to the repository</param>
        /// <returns>true if the path should be ignored.</returns>
        public virtual bool IsPathIgnored(string relativePath)
        {
            return Proxy.git_ignore_path_is_ignored(repo.Handle, relativePath);
        }
    }
}
