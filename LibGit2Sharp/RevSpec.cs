using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Defines a Git Revision Spec returned by <see cref="RevSpec.Parse"/>.
    /// </summary>
    public sealed class RevSpec
    {
        private RevSpec()
        {
        }

        /// <summary>
        /// Gets the left element of the revspec
        /// </summary>
        /// <value>The left element of the revspec.</value>
        public GitObject From { get; private set; }

        /// <summary>
        /// Gets the right element of the revspec.
        /// </summary>
        /// <value>The right element of the revspec.</value>
        public GitObject To { get; private set; }

        /// <summary>
        /// Gets the intent of the revspec .
        /// </summary>
        /// <value>The intent of the revspec .</value>
        public RevSpecType Type { get; private set; }

        /// <summary>
        /// Parse a revision string for `from`, `to`, and intent.
        /// </summary>
        /// <param name="repo">The repository to search in.</param>
        /// <param name="spec">The rev-parse spec to parse.</param>
        /// <returns>The result of rev-parse.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// repo
        /// or
        /// spec
        /// </exception>
        public static RevSpec Parse(Repository repo, string spec)
        {
            if (repo == null)
            {
                throw new ArgumentNullException("repo");
            }
            if (spec == null)
            {
                throw new ArgumentNullException("spec");
            }

            GitRevSpec gitRevSpec;

            var result = NativeMethods.git_revparse(out gitRevSpec, repo.Handle, spec);
            Ensure.Int32Result(result);

            var revSpec = new RevSpec();

            if (gitRevSpec.From != IntPtr.Zero)
            {
                using (var sh = new GitObjectSafeHandle(gitRevSpec.From))
                {
                    var objType = Proxy.git_object_type(sh);
                    revSpec.From = GitObject.BuildFrom(repo, Proxy.git_object_id(sh), objType, null);
                }
            }

            if (gitRevSpec.To != IntPtr.Zero)
            {
                using (var sh = new GitObjectSafeHandle(gitRevSpec.To))
                {
                    var objType = Proxy.git_object_type(sh);
                    revSpec.To = GitObject.BuildFrom(repo, Proxy.git_object_id(sh), objType, null);
                }
            }

            revSpec.Type = gitRevSpec.Type;

            return revSpec;
        }
    }
}