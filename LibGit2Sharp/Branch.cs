﻿using System;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A branch is a special kind of reference
    /// </summary>
    public class Branch : ReferenceWrapper<Commit>, IBranch
    {
        private readonly Lazy<IBranch> trackedBranch;

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
            trackedBranch = new Lazy<IBranch>(ResolveTrackedBranch);
        }

        /// <summary>
        ///   Gets the <see cref = "TreeEntry" /> pointed at by the <paramref name = "relativePath" /> in the <see cref = "Tip" />.
        /// </summary>
        /// <param name = "relativePath">The relative path to the <see cref = "TreeEntry" /> from the <see cref = "Tip" /> working directory.</param>
        /// <returns><c>null</c> if nothing has been found, the <see cref = "TreeEntry" /> otherwise.</returns>
        public TreeEntry this[string relativePath]
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
        ///   Gets the remote branch which is connected to this local one.
        /// </summary>
        public IBranch TrackedBranch
        {
            get { return trackedBranch.Value; }
        }

        /// <summary>
        ///   Determines if this local branch is connected to a remote one.
        /// </summary>
        public bool IsTracking
        {
            get { return TrackedBranch != null; }
        }

        /// <summary>
        ///   Gets the number of commits, starting from the <see cref="Tip"/>, that have been performed on this local branch and aren't known from the remote one.
        /// </summary>
        public int AheadBy
        {
            get { return IsTracking ? repo.Commits.QueryBy(new Filter { Since = Tip, Until = TrackedBranch }).Count() : 0; }
        }

        /// <summary>
        ///   Gets the number of commits that exist in the remote branch, on top of <see cref="Tip"/>, and aren't known from the local one.
        /// </summary>
        public int BehindBy
        {
            get { return IsTracking ? repo.Commits.QueryBy(new Filter { Since = TrackedBranch, Until = Tip }).Count() : 0; }
        }

        /// <summary>
        ///   Gets a value indicating whether this instance is current branch (HEAD) in the repository.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is the current branch; otherwise, <c>false</c>.
        /// </value>
        public bool IsCurrentRepositoryHead
        {
            get { return repo.Head.Equals(this); }
        }

        /// <summary>
        ///   Gets the <see cref="Commit"/> that this branch points to.
        /// </summary>
        public ICommit Tip
        {
            get { return TargetObject; }
        }

        /// <summary>
        ///   Gets the commits on this branch. (Starts walking from the References's target).
        /// </summary>
        public ICommitLog Commits
        {
            get { return repo.Commits.QueryBy(new Filter {Since = this}); }
        }

        private IBranch ResolveTrackedBranch()
        {
            var trackedRemote = repo.Config.Get<string>("branch", Name, "remote", null);
            if (trackedRemote == null)
            {
                return null;
            }

            var trackedRefName = repo.Config.Get<string>("branch", Name, "merge", null);
            if (trackedRefName == null)
            {
                return null;
            }

            var remoteRefName = ResolveTrackedReference(trackedRemote, trackedRefName);
            return repo.Branches[remoteRefName];
        }

        private static string ResolveTrackedReference(string trackedRemote, string trackedRefName)
        {
            if (trackedRemote == ".")
            {
                return trackedRefName;
            }

            //TODO: To be replaced by native libgit2 git_branch_tracked_reference() when available.
            return trackedRefName.Replace("refs/heads/", string.Concat("refs/remotes/", trackedRemote, "/"));
        }

        private static bool IsRemoteBranch(string canonicalName)
        {
            return canonicalName.StartsWith("refs/remotes/", StringComparison.Ordinal);
        }

        /// <summary>
        ///   Returns the friendly shortened name from a canonical name.
        /// </summary>
        /// <param name="canonicalName">The canonical name to shorten.</param>
        /// <returns></returns>
        protected override string Shorten(string canonicalName)
        {
            if (canonicalName.StartsWith("refs/heads/", StringComparison.Ordinal))
            {
                return canonicalName.Substring("refs/heads/".Length);
            }

            if (canonicalName.StartsWith("refs/remotes/", StringComparison.Ordinal))
            {
                return canonicalName.Substring("refs/remotes/".Length);
            }

            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "'{0}' does not look like a valid branch name.", canonicalName));
        }
    }
}
