using System;
using LibGit2Sharp;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using System.IO;

namespace LibGit2Sharp
{
    /// <summary>
    /// Fetch changes from the configured upstream remote and branch into the branch pointed at by HEAD.
    /// </summary>
    public static partial class Commands
    {
        /// <summary>
        /// Adds a new repository, checkout the selected branch and add it to superproject index  
        /// </summary>
        /// <param name="repository">The repository to add the Submodule to</param>
        /// <param name="name">The name of the Submodule</param>
        /// <param name="url">The url of the remote repository</param>
        /// <param name="relativePath">The path of the submodule inside of the super repository, if none, name is taken.</param>
        /// <param name="useGitLink">Should workdir contain a gitlink to the repo in .git/modules vs. repo directly in workdir.</param>
        /// <param name="initiRepository">Should workdir contain a gitlink to the repo in .git/modules vs. repo directly in workdir.</param>
        /// <returns></returns>
        public static Submodule AddSubmodule(IRepository repository, string name, string url, string relativePath, bool useGitLink, Action<Repository> initiRepository)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            Ensure.ArgumentNotNullOrEmptyString(url, "url");

            relativePath = relativePath ?? name;

            using (SubmoduleHandle handle = Proxy.git_submodule_add_setup(((Repository)repository).Handle, url, relativePath, useGitLink ? 1 : 0))
            {
                string subPath = Path.Combine(repository.Info.WorkingDirectory, relativePath);

                using (Repository subRep = new Repository(subPath))
                {
                    initiRepository(subRep);
                }

                Proxy.git_submodule_add_finalize(handle);
            }

            return repository.Submodules[name];
        }
    }
}

