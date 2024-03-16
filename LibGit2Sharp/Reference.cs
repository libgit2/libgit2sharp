using System;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A Reference to another git object
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class Reference : IEquatable<Reference>, IBelongToARepository
    {
        private static readonly LambdaEqualityHelper<Reference> equalityHelper =
            new LambdaEqualityHelper<Reference>(x => x.CanonicalName, x => x.TargetIdentifier);

        private readonly IRepository repo;
        private readonly string canonicalName;
        private readonly string targetIdentifier;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Reference()
        { }

        /// <remarks>
        /// This would be protected+internal, were that supported by C#.
        /// Do not use except in subclasses.
        /// </remarks>
        internal Reference(IRepository repo, string canonicalName, string targetIdentifier)
        {
            this.repo = repo;
            this.canonicalName = canonicalName;
            this.targetIdentifier = targetIdentifier;
        }

        // This overload lets public-facing methods avoid having to use the pointers directly
        internal static unsafe T BuildFromPtr<T>(ReferenceHandle handle, Repository repo) where T : Reference
        {
            return BuildFromPtr<T>((git_reference*) handle.Handle, repo);
        }

        internal static unsafe T BuildFromPtr<T>(git_reference* handle, Repository repo) where T : Reference
        {
            GitReferenceType type = Proxy.git_reference_type(handle);
            string name = Proxy.git_reference_name(handle);

            Reference reference;

            switch (type)
            {
                case GitReferenceType.Symbolic:
                    string targetIdentifier = Proxy.git_reference_symbolic_target(handle);

                    var targetRef = repo.Refs[targetIdentifier];
                    reference = new SymbolicReference(repo, name, targetIdentifier, targetRef);
                    break;

                case GitReferenceType.Oid:
                    ObjectId targetOid = Proxy.git_reference_target(handle);

                    reference = new DirectReference(name, repo, targetOid);
                    break;

                default:
                    throw new LibGit2SharpException("Unable to build a new reference from a type '{0}'.", type);
            }

            return reference as T;
        }

        /// <summary>
        /// Determines if the proposed reference name is well-formed.
        /// </summary>
        /// <para>
        /// - Top-level names must contain only capital letters and underscores,
        /// and must begin and end with a letter. (e.g. "HEAD", "ORIG_HEAD").
        ///
        /// - Names prefixed with "refs/" can be almost anything.  You must avoid
        /// the characters '~', '^', ':', '\\', '?', '[', and '*', and the
        /// sequences ".." and "@{" which have special meaning to revparse.
        /// </para>
        /// <param name="canonicalName">The name to be checked.</param>
        /// <returns>true is the name is valid; false otherwise.</returns>
        public static bool IsValidName(string canonicalName)
        {
            return Proxy.git_reference_is_valid_name(canonicalName);
        }

        /// <summary>
        /// Determine if the current <see cref="Reference"/> is a local branch.
        /// </summary>
        /// <returns>true if the current <see cref="Reference"/> is a local branch, false otherwise.</returns>
        public virtual bool IsLocalBranch
        {
            get { return CanonicalName.LooksLikeLocalBranch(); }
        }

        /// <summary>
        /// Determine if the current <see cref="Reference"/> is a remote tracking branch.
        /// </summary>
        /// <returns>true if the current <see cref="Reference"/> is a remote tracking branch, false otherwise.</returns>
        public virtual bool IsRemoteTrackingBranch
        {
            get { return CanonicalName.LooksLikeRemoteTrackingBranch(); }
        }

        /// <summary>
        /// Determine if the current <see cref="Reference"/> is a tag.
        /// </summary>
        /// <returns>true if the current <see cref="Reference"/> is a tag, false otherwise.</returns>
        public virtual bool IsTag
        {
            get { return CanonicalName.LooksLikeTag(); }
        }

        /// <summary>
        /// Determine if the current <see cref="Reference"/> is a note.
        /// </summary>
        /// <returns>true if the current <see cref="Reference"/> is a note, false otherwise.</returns>
        public virtual bool IsNote
        {
            get { return CanonicalName.LooksLikeNote(); }
        }

        /// <summary>
        /// Gets the full name of this reference.
        /// </summary>
        public virtual string CanonicalName
        {
            get { return canonicalName; }
        }

        /// <summary>
        /// Recursively peels the target of the reference until a direct reference is encountered.
        /// </summary>
        /// <returns>The <see cref="DirectReference"/> this <see cref="Reference"/> points to.</returns>
        public abstract DirectReference ResolveToDirectReference();

        /// <summary>
        /// Gets the target declared by the reference.
        /// <para>
        ///   If this reference is a <see cref="SymbolicReference"/>, returns the canonical name of the target.
        ///   Otherwise, if this reference is a <see cref="DirectReference"/>, returns the sha of the target.
        /// </para>
        /// </summary>
        // TODO: Maybe find a better name for this property.
        public virtual string TargetIdentifier
        {
            get { return targetIdentifier; }
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="Reference"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="Reference"/>.</param>
        /// <returns>True if the specified <see cref="object"/> is equal to the current <see cref="Reference"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Reference);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Reference"/> is equal to the current <see cref="Reference"/>.
        /// </summary>
        /// <param name="other">The <see cref="Reference"/> to compare with the current <see cref="Reference"/>.</param>
        /// <returns>True if the specified <see cref="Reference"/> is equal to the current <see cref="Reference"/>; otherwise, false.</returns>
        public bool Equals(Reference other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        /// Tests if two <see cref="Reference"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="Reference"/> to compare.</param>
        /// <param name="right">Second <see cref="Reference"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Reference left, Reference right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="Reference"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="Reference"/> to compare.</param>
        /// <param name="right">Second <see cref="Reference"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Reference left, Reference right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Returns the <see cref="CanonicalName"/>, a <see cref="string"/> representation of the current <see cref="Reference"/>.
        /// </summary>
        /// <returns>The <see cref="CanonicalName"/> that represents the current <see cref="Reference"/>.</returns>
        public override string ToString()
        {
            return CanonicalName;
        }

        internal static string LocalBranchPrefix
        {
            get { return "refs/heads/"; }
        }

        internal static string RemoteTrackingBranchPrefix
        {
            get { return "refs/remotes/"; }
        }

        internal static string TagPrefix
        {
            get { return "refs/tags/"; }
        }

        internal static string NotePrefix
        {
            get { return "refs/notes/"; }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "{0} => \"{1}\"",
                                     CanonicalName,
                                     TargetIdentifier);
            }
        }

        IRepository IBelongToARepository.Repository
        {
            get
            {
                if (repo == null)
                {
                    throw new InvalidOperationException("Repository requires a local repository");
                }

                return repo;
            }
        }
    }
}
