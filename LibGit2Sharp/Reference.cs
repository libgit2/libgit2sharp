using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Reference to another git object
    /// </summary>
    public abstract class Reference
    {
        private IntPtr referencePtr;

        /// <summary>
        ///   Gets the name of this reference.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///   Gets the type of this reference.
        /// </summary>
        public GitReferenceType Type { get; private set; }

        internal static Reference CreateFromPtr(IntPtr ptr, Repository repo)
        {
            var name = NativeMethods.git_reference_name(ptr);
            var type = NativeMethods.git_reference_type(ptr);
            if (type == GitReferenceType.Symbolic)
            {
                IntPtr resolveRef;
                NativeMethods.git_reference_resolve(out resolveRef, ptr);
                var reference = CreateFromPtr(resolveRef, repo);
                return new SymbolicReference {Name = name, Type = type, Target = reference, referencePtr = ptr};
            }
            if (type == GitReferenceType.Oid)
            {
                var oidPtr = NativeMethods.git_reference_oid(ptr);
                var oid = (GitOid) Marshal.PtrToStructure(oidPtr, typeof (GitOid));
                var target = repo.Lookup(new ObjectId(oid));
                return new DirectReference {Name = name, Type = type, Target = target, referencePtr = ptr};
            }
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Deletes this reference.
        /// </summary>
        public void Delete()
        {
            var res = NativeMethods.git_reference_delete(referencePtr);
            Ensure.Success(res);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(Reference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Name, Name);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

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
    }
}