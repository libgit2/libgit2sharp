using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Reference to another git object
    /// </summary>
    public abstract class Reference : IEquatable<Reference>
    {
        /// <summary>
        ///   Gets the name of this reference.
        /// </summary>
        public string Name { get; private set; }

        #region IEquatable<Reference> Members

        public bool Equals(Reference other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (GetType() != other.GetType())
            {
                return false;
            }

            if (!Equals(Name, other.Name))
            {
                return false;
            }

            return Equals(ProvideAdditionalEqualityComponent(), other.ProvideAdditionalEqualityComponent());
        }

        #endregion

        internal static Reference CreateFromPtr(IntPtr ptr, Repository repo)
        {
            var name = NativeMethods.git_reference_name(ptr);
            var type = NativeMethods.git_reference_type(ptr);
            
            switch (type)
            {
                case GitReferenceType.Symbolic:
                    IntPtr resolveRef;
                    NativeMethods.git_reference_resolve(out resolveRef, ptr);
                    var reference = CreateFromPtr(resolveRef, repo);
                    return new SymbolicReference {Name = name, Target = reference};

                case GitReferenceType.Oid:
                    var oidPtr = NativeMethods.git_reference_oid(ptr);
                    var oid = (GitOid) Marshal.PtrToStructure(oidPtr, typeof (GitOid));
                    var target = repo.Lookup(new ObjectId(oid));
                    return new DirectReference {Name = name, Target = target};
                
                default:
                    throw new InvalidOperationException();
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Reference);
        }

        public override int GetHashCode()
        {
            int hashCode = GetType().GetHashCode();

            unchecked
            {
                hashCode = (hashCode*397) ^ Name.GetHashCode();
                hashCode = (hashCode*397) ^ ProvideAdditionalEqualityComponent().GetHashCode();
            }

            return hashCode;
        }

        protected abstract object ProvideAdditionalEqualityComponent();

        /// <summary>
        ///   Resolves to direct reference.
        /// </summary>
        /// <returns></returns>
        public DirectReference ResolveToDirectReference()
        {
            return ResolveToDirectReference(this);
        }

        private static DirectReference ResolveToDirectReference(Reference reference)
        {
            if (reference is DirectReference) return (DirectReference) reference;
            return ResolveToDirectReference(((SymbolicReference) reference).Target);
        }

        public static bool operator ==(Reference left, Reference right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Reference left, Reference right)
        {
            return !Equals(left, right);
        }
    }
}