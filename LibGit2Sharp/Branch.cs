using System;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A branch is a special kind of reference
    /// </summary>
    public class Branch : ReferenceWrapper<Commit>
    {
        private readonly Lazy<Branch> trackedBranch;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Branch()
        { }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Branch" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        /// <param name = "reference">The reference.</param>
        /// <param name = "canonicalName">The full name of the reference</param>
        internal Branch(Repository repo, Reference reference, string canonicalName)
            : this(repo, reference, _ => canonicalName)
        {
        }

        /// <summary>
        ///   Initializes a new instance of an orphaned <see cref = "Branch" /> class.
        ///   <para>
        ///     This <see cref = "Branch" /> instance will point to no commit.
        ///   </para>
        /// </summary>
        /// <param name = "repo">The repo.</param>
        /// <param name = "reference">The reference.</param>
        internal Branch(Repository repo, Reference reference)
            : this(repo, reference, r => r.TargetIdentifier)
        {
        }

        private Branch(Repository repo, Reference reference, Func<Reference, string> canonicalNameSelector)
            : base(repo, reference, canonicalNameSelector)
        {
            trackedBranch = new Lazy<Branch>(ResolveTrackedBranch);
        }

        /// <summary>
        ///   Gets the <see cref = "TreeEntry" /> pointed at by the <paramref name = "relativePath" /> in the <see cref = "Tip" />.
        /// </summary>
        /// <param name = "relativePath">The relative path to the <see cref = "TreeEntry" /> from the <see cref = "Tip" /> working directory.</param>
        /// <returns><c>null</c> if nothing has been found, the <see cref = "TreeEntry" /> otherwise.</returns>
        public virtual TreeEntry this[string relativePath]
        {
            get
            {
                if (Tip == null)
                {
                    return null;
                }

                return Tip[relativePath];
            }
        }

        /// <summary>
        ///   Gets a value indicating whether this instance is a remote.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is remote; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsRemote
        {
            get { return IsRemoteBranch(CanonicalName); }
        }

        /// <summary>
        ///   Gets the remote branch which is connected to this local one, or null if there is none.
        /// </summary>
        public virtual Branch TrackedBranch
        {
            get { return trackedBranch.Value; }
        }

        /// <summary>
        ///   Determines if this local branch is connected to a remote one.
        /// </summary>
        public virtual bool IsTracking
        {
            get { return TrackedBranch != null; }
        }

        private bool ExistsPathToTrackedBranch()
        {
            if (!IsTracking)
            {
                return false;
            }

            if (repo.Commits.FindCommonAncestor(Tip, TrackedBranch.Tip) == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///   Gets the number of commits, starting from the <see cref="Tip"/>, that have been performed on this local branch and aren't known from the remote one.
        ///   <para>
        ///     This property will return null if there is no remote branch linked to this local branch, or if the remote branch and the local branch do
        ///     not share a common ancestor.
        ///   </para>
        /// </summary>
        public virtual int? AheadBy
        {
            get { return ExistsPathToTrackedBranch() ? repo.Commits.QueryBy(new Filter { Since = Tip, Until = TrackedBranch }).Count() : (int?)null; }
        }

        /// <summary>
        ///   Gets the number of commits that exist in the remote branch, on top of <see cref="Tip"/>, and aren't known from the local one.
        ///   <para>
        ///     This property will return null if there is no remote branch linked to this local branch, or if the remote branch and the local branch do
        ///     not share a common ancestor.
        ///   </para>
        /// </summary>
        public virtual int? BehindBy
        {
            get { return ExistsPathToTrackedBranch() ? repo.Commits.QueryBy(new Filter { Since = TrackedBranch, Until = Tip }).Count() : (int?)null; }
        }

        /// <summary>
        ///   Gets a value indicating whether this instance is current branch (HEAD) in the repository.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is the current branch; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsCurrentRepositoryHead
        {
            get { return repo.Head == this; }
        }

        /// <summary>
        ///   Gets the <see cref="Commit"/> that this branch points to.
        /// </summary>
        public virtual Commit Tip
        {
            get { return TargetObject; }
        }

        /// <summary>
        ///   Gets the commits on this branch. (Starts walking from the References's target).
        /// </summary>
        public virtual ICommitLog Commits
        {
            get { return repo.Commits.QueryBy(new Filter { Since = this }); }
        }

        /// <summary>
        ///   Gets the configured <see cref="Remote"/> to fetch from and push to.
        /// </summary>
        public virtual Remote Remote
        {
            get
            {
                ConfigurationEntry<string> remoteEntry = repo.Config.Get<string>("branch", Name, "remote");

                if (remoteEntry == null)
                {
                    return null;
                }

                string remoteName = remoteEntry.Value;

                if (string.IsNullOrEmpty(remoteName) ||
                    string.Equals(remoteName, ".", StringComparison.Ordinal))
                {
                    return null;
                }

                return repo.Remotes[remoteName];
            }
        }

        /// <summary>
        ///   Checkout this branch.
        /// </summary>
        public virtual void Checkout()
        {
            repo.Checkout(this);
        }

        /// <summary>
        ///   Checkout this branch with a callback for progress reporting.
        /// </summary>
        /// <param name="checkoutOptions">Options controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress">Callback method to report checkout progress updates through.</param>
        public virtual void Checkout(CheckoutOptions checkoutOptions, CheckoutProgressHandler onCheckoutProgress)
        {
            repo.Checkout(this, checkoutOptions, onCheckoutProgress);
        }

        private Branch ResolveTrackedBranch()
        {
            using (ReferenceSafeHandle branchPtr = repo.Refs.RetrieveReferencePtr(CanonicalName, false))
            {
                if (branchPtr == null)
                {
                    return null;
                }

                using (ReferenceSafeHandle referencePtr = Proxy.git_branch_tracking(branchPtr))
                {
                    if (referencePtr == null)
                    {
                        return null;
                    }

                    var reference = Reference.BuildFromPtr<Reference>(referencePtr, repo);
                    return repo.Branches[reference.CanonicalName];
                }
            }
        }

        private static bool IsRemoteBranch(string canonicalName)
        {
            return canonicalName.StartsWith("refs/remotes/", StringComparison.Ordinal);
        }

        /// <summary>
        ///   Removes redundent leading namespaces (regarding the kind of
        ///   reference being wrapped) from the canonical name.
        /// </summary>
        /// <returns>The friendly shortened name</returns>
        protected override string Shorten()
        {
            if (CanonicalName.StartsWith("refs/heads/", StringComparison.Ordinal))
            {
                return CanonicalName.Substring("refs/heads/".Length);
            }

            if (CanonicalName.StartsWith("refs/remotes/", StringComparison.Ordinal))
            {
                return CanonicalName.Substring("refs/remotes/".Length);
            }

            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture,
                    "'{0}' does not look like a valid branch name.", CanonicalName));
        }
    }
}
