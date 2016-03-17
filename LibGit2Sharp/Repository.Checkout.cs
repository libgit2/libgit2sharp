using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public partial class Repository
    {
        /// <summary>
        /// Checkout the specified <see cref="Branch"/>, reference or SHA.
        /// <para>
        ///   If the committishOrBranchSpec parameter resolves to a branch name, then the checked out HEAD will
        ///   will point to the branch. Otherwise, the HEAD will be detached, pointing at the commit sha.
        /// </para>
        /// </summary>
        /// <param name="committishOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <param name="options"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public Branch Checkout(string committishOrBranchSpec, CheckoutOptions options)
        {
            Ensure.ArgumentNotNullOrEmptyString(committishOrBranchSpec, "committishOrBranchSpec");
            Ensure.ArgumentNotNull(options, "options");

            var handles = Proxy.git_revparse_ext(Handle, committishOrBranchSpec);
            if (handles == null)
            {
                Ensure.GitObjectIsNotNull(null, committishOrBranchSpec);
            }

            var objH = handles.Item1;
            var refH = handles.Item2;
            GitObject obj;
            try
            {
                if (!refH.IsInvalid)
                {
                    var reference = Reference.BuildFromPtr<Reference>(refH, this);
                    if (reference.IsLocalBranch)
                    {
                        Branch branch = Branches[reference.CanonicalName];
                        return Checkout(branch, options);
                    }
                }

                obj = GitObject.BuildFrom(this,
                    Proxy.git_object_id(objH),
                    Proxy.git_object_type(objH),
                    PathFromRevparseSpec(committishOrBranchSpec));
            }
            finally
            {
                objH.Dispose();
                refH.Dispose();
            }

            Commit commit = obj.DereferenceToCommit(true);
            Checkout(commit.Tree, options, committishOrBranchSpec);

            return Head;
        }

        /// <summary>
        /// Checkout the tip commit of the specified <see cref="Branch"/> object. If this commit is the
        /// current tip of the branch, will checkout the named branch. Otherwise, will checkout the tip commit
        /// as a detached HEAD.
        /// </summary>
        /// <param name="branch">The <see cref="Branch"/> to check out.</param>
        /// <param name="options"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public Branch Checkout(Branch branch, CheckoutOptions options)
        {
            Ensure.ArgumentNotNull(branch, "branch");
            Ensure.ArgumentNotNull(options, "options");

            // Make sure this is not an unborn branch.
            if (branch.Tip == null)
            {
                throw new UnbornBranchException("The tip of branch '{0}' is null. There's nothing to checkout.",
                    branch.FriendlyName);
            }

            if (!branch.IsRemote && !(branch is DetachedHead) &&
                string.Equals(Refs[branch.CanonicalName].TargetIdentifier, branch.Tip.Id.Sha,
                    StringComparison.OrdinalIgnoreCase))
            {
                Checkout(branch.Tip.Tree, options, branch.CanonicalName);
            }
            else
            {
                Checkout(branch.Tip.Tree, options, branch.Tip.Id.Sha);
            }

            return Head;
        }

        /// <summary>
        /// Checkout the specified <see cref="LibGit2Sharp.Commit"/>.
        /// <para>
        ///   Will detach the HEAD and make it point to this commit sha.
        /// </para>
        /// </summary>
        /// <param name="commit">The <see cref="LibGit2Sharp.Commit"/> to check out.</param>
        /// <param name="options"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public Branch Checkout(Commit commit, CheckoutOptions options)
        {
            Ensure.ArgumentNotNull(commit, "commit");
            Ensure.ArgumentNotNull(options, "options");

            Checkout(commit.Tree, options, commit.Id.Sha);

            return Head;
        }

        /// <summary>
        /// Internal implementation of Checkout that expects the ID of the checkout target
        /// to already be in the form of a canonical branch name or a commit ID.
        /// </summary>
        /// <param name="tree">The <see cref="Tree"/> to checkout.</param>
        /// <param name="checkoutOptions"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <param name="refLogHeadSpec">The spec which will be written as target in the reflog.</param>
        private void Checkout(
            Tree tree,
            CheckoutOptions checkoutOptions,
            string refLogHeadSpec)
        {
            CheckoutTree(tree, null, checkoutOptions);

            Refs.MoveHeadTarget(refLogHeadSpec);
        }

        /// <summary>
        /// Checkout the specified tree.
        /// </summary>
        /// <param name="tree">The <see cref="Tree"/> to checkout.</param>
        /// <param name="paths">The paths to checkout.</param>
        /// <param name="opts">Collection of parameters controlling checkout behavior.</param>
        private void CheckoutTree(
            Tree tree,
            IList<string> paths,
            IConvertableToGitCheckoutOpts opts)
        {

            using (GitCheckoutOptsWrapper checkoutOptionsWrapper = new GitCheckoutOptsWrapper(opts, ToFilePaths(paths)))
            {
                var options = checkoutOptionsWrapper.Options;
                Proxy.git_checkout_tree(Handle, tree.Id, ref options);
            }
        }

        /// <summary>
        /// Checkout the specified <see cref="Branch"/>, reference or SHA.
        /// </summary>
        /// <param name="commitOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public Branch Checkout(string commitOrBranchSpec)
        {
            CheckoutOptions options = new CheckoutOptions();
            return Checkout(commitOrBranchSpec, options);
        }

        /// <summary>
        /// Checkout the commit pointed at by the tip of the specified <see cref="Branch"/>.
        /// <para>
        ///   If this commit is the current tip of the branch as it exists in the repository, the HEAD
        ///   will point to this branch. Otherwise, the HEAD will be detached, pointing at the commit sha.
        /// </para>
        /// </summary>
        /// <param name="branch">The <see cref="Branch"/> to check out.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public Branch Checkout(Branch branch)
        {
            CheckoutOptions options = new CheckoutOptions();
            return Checkout(branch, options);
        }

        /// <summary>
        /// Checkout the specified <see cref="LibGit2Sharp.Commit"/>.
        /// <para>
        ///   Will detach the HEAD and make it point to this commit sha.
        /// </para>
        /// </summary>
        /// <param name="commit">The <see cref="LibGit2Sharp.Commit"/> to check out.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public Branch Checkout(Commit commit)
        {
            CheckoutOptions options = new CheckoutOptions();
            return Checkout(commit, options);
        }

        /// <summary>
        /// Updates specifed paths in the index and working directory with the versions from the specified branch, reference, or SHA.
        /// <para>
        /// This method does not switch branches or update the current repository HEAD.
        /// </para>
        /// </summary>
        /// <param name = "committishOrBranchSpec">A revparse spec for the commit or branch to checkout paths from.</param>
        /// <param name="paths">The paths to checkout. Will throw if null is passed in. Passing an empty enumeration results in nothing being checked out.</param>
        /// <param name="checkoutOptions">Collection of parameters controlling checkout behavior.</param>
        public void CheckoutPaths(string committishOrBranchSpec, IEnumerable<string> paths, CheckoutOptions checkoutOptions)
        {
            Ensure.ArgumentNotNullOrEmptyString(committishOrBranchSpec, "committishOrBranchSpec");
            Ensure.ArgumentNotNull(paths, "paths");

            var listOfPaths = paths.ToList();

            // If there are no paths, then there is nothing to do.
            if (listOfPaths.Count == 0)
            {
                return;
            }

            Commit commit = LookupCommit(committishOrBranchSpec);

            CheckoutTree(commit.Tree, listOfPaths, checkoutOptions ?? new CheckoutOptions());
        }

        /// <summary>
        /// Updates specifed paths in the index and working directory with the versions from the specified branch, reference, or SHA.
        /// <para>
        /// This method does not switch branches or update the current repository HEAD.
        /// </para>
        /// </summary>
        /// <param name = "committishOrBranchSpec">A revparse spec for the commit or branch to checkout paths from.</param>
        /// <param name="paths">The paths to checkout. Will throw if null is passed in. Passing an empty enumeration results in nothing being checked out.</param>
        public void CheckoutPaths(string committishOrBranchSpec, IEnumerable<string> paths)
        {
            CheckoutPaths(committishOrBranchSpec, paths, null);
        }

        internal string BuildRelativePathFrom(string path)
        {
            //TODO: To be removed when libgit2 natively implements this
            if (!Path.IsPathRooted(path))
            {
                return path;
            }

            string normalizedPath = Path.GetFullPath(path);

            if (!PathStartsWith(normalizedPath, Info.WorkingDirectory))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "Unable to process file '{0}'. This file is not located under the working directory of the repository ('{1}').",
                    normalizedPath,
                    Info.WorkingDirectory));
            }

            return normalizedPath.Substring(Info.WorkingDirectory.Length);
        }
    }
}

