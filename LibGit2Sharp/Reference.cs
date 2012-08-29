using System;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Reference to another git object
    /// </summary>
    public abstract class Reference : IEquatable<Reference>
    {
        private static readonly LambdaEqualityHelper<Reference> equalityHelper =
            new LambdaEqualityHelper<Reference>(new Func<Reference, object>[] { x => x.CanonicalName, x => x.TargetIdentifier });

        /// <summary>
        ///   Gets the full name of this reference.
        /// </summary>
        public virtual string CanonicalName { get; protected set; }

        internal static T BuildFromPtr<T>(ReferenceSafeHandle handle, Repository repo) where T : Reference
        {
            GitReferenceType type = Proxy.git_reference_type(handle);
            string name = Proxy.git_reference_name(handle);

            Reference reference;

            switch (type)
            {
                case GitReferenceType.Symbolic:
                    string targetIdentifier = Proxy.git_reference_target(handle);

                    using (ReferenceSafeHandle resolvedHandle = Proxy.git_reference_resolve(handle))
                    {
                        if (resolvedHandle == null)
                        {
                            reference = new SymbolicReference { CanonicalName = name, Target = null, TargetIdentifier = targetIdentifier };
                            break;
                        }

                        var targetRef = BuildFromPtr<DirectReference>(resolvedHandle, repo);
                        reference = new SymbolicReference { CanonicalName = name, Target = targetRef, TargetIdentifier = targetIdentifier };
                        break;
                    }

                case GitReferenceType.Oid:
                    ObjectId targetOid = Proxy.git_reference_oid(handle);

                    var targetBuilder = new Lazy<GitObject>(() => repo.Lookup(targetOid));
                    reference = new DirectReference(targetBuilder) { CanonicalName = name, TargetIdentifier = targetOid.Sha };
                    break;

                default:
                    throw new LibGit2SharpException(String.Format(CultureInfo.InvariantCulture, "Unable to build a new reference from a type '{0}'.", type));
            }

            return reference as T;
        }

        /// <summary>
        ///   Recursively peels the target of the reference until a direct reference is encountered.
        /// </summary>
        /// <returns>The <see cref = "DirectReference" /> this <see cref = "Reference" /> points to.</returns>
        public abstract DirectReference ResolveToDirectReference();

        /// <summary>
        ///   Gets the target declared by the reference.
        ///   <para>
        ///     If this reference is a <see cref = "SymbolicReference" />, returns the canonical name of the target.
        ///     Otherwise, if this reference is a <see cref = "DirectReference" />, returns the sha of the target.
        ///   </para>
        /// </summary>
        // TODO: Maybe find a better name for this property.
        public virtual string TargetIdentifier { get; private set; }

        /// <summary>
        ///   Determines whether the specified <see cref = "Object" /> is equal to the current <see cref = "Reference" />.
        /// </summary>
        /// <param name = "obj">The <see cref = "Object" /> to compare with the current <see cref = "Reference" />.</param>
        /// <returns>True if the specified <see cref = "Object" /> is equal to the current <see cref = "Reference" />; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Reference);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "Reference" /> is equal to the current <see cref = "Reference" />.
        /// </summary>
        /// <param name = "other">The <see cref = "Reference" /> to compare with the current <see cref = "Reference" />.</param>
        /// <returns>True if the specified <see cref = "Reference" /> is equal to the current <see cref = "Reference" />; otherwise, false.</returns>
        public bool Equals(Reference other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        ///   Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        ///   Tests if two <see cref = "Reference" /> are equal.
        /// </summary>
        /// <param name = "left">First <see cref = "Reference" /> to compare.</param>
        /// <param name = "right">Second <see cref = "Reference" /> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Reference left, Reference right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Tests if two <see cref = "Reference" /> are different.
        /// </summary>
        /// <param name = "left">First <see cref = "Reference" /> to compare.</param>
        /// <param name = "right">Second <see cref = "Reference" /> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Reference left, Reference right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        ///   Returns the <see cref = "CanonicalName" />, a <see cref = "String" /> representation of the current <see cref = "Reference" />.
        /// </summary>
        /// <returns>The <see cref = "CanonicalName" /> that represents the current <see cref = "Reference" />.</returns>
        public override string ToString()
        {
            return CanonicalName;
        }
    }
}
