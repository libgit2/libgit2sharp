using System;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A branch is a special kind of reference
    /// </summary>
    public class Branch : IEquatable<Branch>
    {
        private static readonly LambdaEqualityHelper<Branch> equalityHelper =
            new LambdaEqualityHelper<Branch>(new Func<Branch, object>[] {x => x.CanonicalName, x => x.Tip});

        private readonly Repository repo;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Branch" /> class.
        /// </summary>
        /// <param name = "tip">The commit which is pointed at by this Branch</param>
        /// <param name = "repo">The repo.</param>
        /// <param name = "canonicalName">The full name of the reference</param>
        internal Branch(string canonicalName, Commit tip, Repository repo)
        {
            this.repo = repo;
            CanonicalName = canonicalName;
            Tip = tip;
        }

        /// <summary>
        ///   Gets the full name of this branch.
        /// </summary>
        public string CanonicalName { get; private set; }

        /// <summary>
        ///   Gets the name of this branch.
        /// </summary>
        public string Name
        {
            get { return ShortenName(CanonicalName); }
        }

        /// <summary>
        ///   Gets a value indicating whether this instance is a remote.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is remote; otherwise, <c>false</c>.
        /// </value>
        public bool IsRemote
        {
            get { return IsRemoteBranch(CanonicalName); }
        }

        /// <summary>
        ///   Gets a value indicating whether this instance is current branch (HEAD) in the repository.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is current branch; otherwise, <c>false</c>.
        /// </value>
        public bool IsCurrentRepositoryHead
        {
            get { return repo.Refs[CanonicalName].ResolveToDirectReference() == repo.Refs["HEAD"].ResolveToDirectReference(); }
        }

        /// <summary>
        ///   Gets the commit id that this branch points to.
        /// </summary>
        public Commit Tip { get; private set; }

        /// <summary>
        ///   Gets the commits on this branch. (Starts walking from the References's target).
        /// </summary>
        public ICommitCollection Commits
        {
            get { return repo.Commits.QueryBy(new Filter{Since = this}); }
        }

        #region IEquatable<Branch> Members

        /// <summary>
        ///   Determines whether the specified <see cref = "Branch" /> is equal to the current <see cref = "Branch" />.
        /// </summary>
        /// <param name = "other">The <see cref = "Branch" /> to compare with the current <see cref = "Branch" />.</param>
        /// <returns>True if the specified <see cref = "Branch" /> is equal to the current <see cref = "Branch" />; otherwise, false.</returns>
        public bool Equals(Branch other)
        {
            return equalityHelper.Equals(this, other);
        }

        #endregion

        /// <summary>
        ///   Determines whether the specified <see cref = "Object" /> is equal to the current <see cref = "Branch" />.
        /// </summary>
        /// <param name = "obj">The <see cref = "Object" /> to compare with the current <see cref = "Branch" />.</param>
        /// <returns>True if the specified <see cref = "Object" /> is equal to the current <see cref = "Branch" />; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Branch);
        }

        /// <summary>
        ///   Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        private static bool IsRemoteBranch(string canonicalName)
        {
            return canonicalName.StartsWith("refs/remotes/", StringComparison.Ordinal);
        }

        private static string ShortenName(string branchName)
        {
            if (branchName.StartsWith("refs/heads/", StringComparison.Ordinal))
            {
                return branchName.Substring("refs/heads/".Length);
            }

            if (branchName.StartsWith("refs/remotes/", StringComparison.Ordinal))
            {
                return branchName.Substring("refs/remotes/".Length);
            }

            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,"'{0}' does not look like a valid branch name.", branchName));
        }

        /// <summary>
        ///   Tests if two <see cref = "Branch" /> are equal.
        /// </summary>
        /// <param name = "left">First <see cref = "Branch" /> to compare.</param>
        /// <param name = "right">Second <see cref = "Branch" /> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Branch left, Branch right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Tests if two <see cref = "Branch" /> are different.
        /// </summary>
        /// <param name = "left">First <see cref = "Branch" /> to compare.</param>
        /// <param name = "right">Second <see cref = "Branch" /> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Branch left, Branch right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        ///  Returns the <see cref="CanonicalName"/>, a <see cref="String"/> representation of the current <see cref="Branch"/>.
        /// </summary>
        /// <returns>The <see cref="CanonicalName"/> that represents the current <see cref="Branch"/>.</returns>
        public override string ToString()
        {
            return CanonicalName;
        }
    }
}