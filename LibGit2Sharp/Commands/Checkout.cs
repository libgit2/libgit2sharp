using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public static partial class Commands
    {
        /// <summary>
        /// Checkout the specified <see cref="Branch"/>, reference or SHA.
        /// <para>
        ///   If the committishOrBranchSpec parameter resolves to a branch name, then the checked out HEAD will
        ///   will point to the branch. Otherwise, the HEAD will be detached, pointing at the commit sha.
        /// </para>
        /// </summary>
        /// <param name="repository">The repository to act on</param>
        /// <param name="committishOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(IRepository repository, string committishOrBranchSpec)
        {
            return Checkout(repository, committishOrBranchSpec, new CheckoutOptions());
        }

        /// <summary>
        /// Checkout the specified <see cref="Branch"/>, reference or SHA.
        /// <para>
        ///   If the committishOrBranchSpec parameter resolves to a branch name, then the checked out HEAD will
        ///   will point to the branch. Otherwise, the HEAD will be detached, pointing at the commit sha.
        /// </para>
        /// </summary>
        /// <param name="repository">The repository to act on</param>
        /// <param name="committishOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <param name="options"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(IRepository repository, string committishOrBranchSpec, CheckoutOptions options)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNullOrEmptyString(committishOrBranchSpec, "committishOrBranchSpec");
            Ensure.ArgumentNotNull(options, "options");

            Reference reference;
            GitObject obj;

            repository.RevParse(committishOrBranchSpec, out reference, out obj);
            if (reference != null && reference.IsLocalBranch)
            {
                Branch branch = repository.Branches[reference.CanonicalName];
                return Checkout(repository, branch, options);
            }

            Commit commit = obj.Peel<Commit>(true);
            Checkout(repository, commit.Tree,  options, committishOrBranchSpec);

            return repository.Head;
        }

        /// <summary>
        /// Checkout the tip commit of the specified <see cref="Branch"/> object. If this commit is the
        /// current tip of the branch, will checkout the named branch. Otherwise, will checkout the tip commit
        /// as a detached HEAD.
        /// </summary>
        /// <param name="repository">The repository to act on</param>
        /// <param name="branch">The <see cref="Branch"/> to check out.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(IRepository repository, Branch branch)
        {
            return Checkout(repository, branch, new CheckoutOptions());
        }

        /// <summary>
        /// Checkout the tip commit of the specified <see cref="Branch"/> object. If this commit is the
        /// current tip of the branch, will checkout the named branch. Otherwise, will checkout the tip commit
        /// as a detached HEAD.
        /// </summary>
        /// <param name="repository">The repository to act on</param>
        /// <param name="branch">The <see cref="Branch"/> to check out.</param>
        /// <param name="options"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(IRepository repository, Branch branch, CheckoutOptions options)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(branch, "branch");
            Ensure.ArgumentNotNull(options, "options");

            // Make sure this is not an unborn branch.
            if (branch.Tip == null)
            {
                throw new UnbornBranchException("The tip of branch '{0}' is null. There's nothing to checkout.",
                    branch.FriendlyName);
            }

            if (!branch.IsRemote && !(branch is DetachedHead) &&
                string.Equals(repository.Refs[branch.CanonicalName].TargetIdentifier, branch.Tip.Id.Sha,
                    StringComparison.OrdinalIgnoreCase))
            {
                Checkout(repository, branch.Tip.Tree, options, branch.CanonicalName);
            }
            else
            {
                Checkout(repository, branch.Tip.Tree, options, branch.Tip.Id.Sha);
            }

            return repository.Head;
        }

        /// <summary>
        /// Checkout the specified <see cref="LibGit2Sharp.Commit"/>.
        /// <para>
        ///   Will detach the HEAD and make it point to this commit sha.
        /// </para>
        /// </summary>
        /// <param name="repository">The repository to act on</param>
        /// <param name="commit">The <see cref="LibGit2Sharp.Commit"/> to check out.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(IRepository repository, Commit commit)
        {
            return Checkout(repository, commit, new CheckoutOptions());
        }

        /// <summary>
        /// Checkout the specified <see cref="LibGit2Sharp.Commit"/>.
        /// <para>
        ///   Will detach the HEAD and make it point to this commit sha.
        /// </para>
        /// </summary>
        /// <param name="repository">The repository to act on</param>
        /// <param name="commit">The <see cref="LibGit2Sharp.Commit"/> to check out.</param>
        /// <param name="options"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(IRepository repository, Commit commit, CheckoutOptions options)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNull(commit, "commit");
            Ensure.ArgumentNotNull(options, "options");

            Checkout(repository, commit.Tree, options, commit.Id.Sha);

            return repository.Head;
        }

        /// <summary>
        /// Internal implementation of Checkout that expects the ID of the checkout target
        /// to already be in the form of a canonical branch name or a commit ID.
        /// </summary>
        /// <param name="repository">The repository to act on</param>
        /// <param name="tree">The <see cref="Tree"/> to checkout.</param>
        /// <param name="checkoutOptions"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <param name="refLogHeadSpec">The spec which will be written as target in the reflog.</param>
        public static void Checkout(IRepository repository, Tree tree, CheckoutOptions checkoutOptions, string refLogHeadSpec)
        {
            repository.Checkout(tree, null, checkoutOptions);

            repository.Refs.MoveHeadTarget(refLogHeadSpec);
        }

    }
}

