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
        private static readonly LambdaEqualityHelper<Reference> equalityHelper =
            new LambdaEqualityHelper<Reference>(new Func<Reference, object>[] { x => x.CanonicalName, x => x.ProvideAdditionalEqualityComponent() });

        /// <summary>
        ///   Gets the full name of this reference.
        /// </summary>
        public string CanonicalName { get; protected set; }

        internal static T BuildFromPtr<T>(IntPtr ptr, Repository repo) where T : class
        {
            var name = NativeMethods.git_reference_name(ptr);
            var type = NativeMethods.git_reference_type(ptr);

            Reference reference;

            switch (type)
            {
                case GitReferenceType.Symbolic:
                    IntPtr resolveRef;
                    NativeMethods.git_reference_resolve(out resolveRef, ptr);
                    var targetRef = BuildFromPtr<Reference>(resolveRef, repo);
                    reference =  new SymbolicReference { CanonicalName = name, Target = targetRef };
                    break;

                case GitReferenceType.Oid:
                    var oidPtr = NativeMethods.git_reference_oid(ptr);
                    var oid = (GitOid)Marshal.PtrToStructure(oidPtr, typeof(GitOid));
                    var target = repo.Lookup(new ObjectId(oid));
                    reference = new DirectReference { CanonicalName = name, Target = target };
                    break;

                default:
                    throw new InvalidOperationException();
            }

            if (typeof(Reference).IsAssignableFrom(typeof(T)))
            {
                return reference as T;
            }

            GitObject targetGitObject = repo.Lookup(reference.ResolveToDirectReference().Target.Id);
            
            if (Equals(typeof(T), typeof(Tag)))
            {
                return new Tag(reference.CanonicalName, targetGitObject, targetGitObject as TagAnnotation) as T;
            }

           if (Equals(typeof(T), typeof(Branch)))
            {
                return new Branch(reference.CanonicalName, targetGitObject as Commit, repo) as T;
            }

            throw new InvalidOperationException(
                string.Format("Unable to build a new instance of '{0}' from a reference of type '{1}'.",
                              typeof (T),
                              Enum.GetName(typeof (GitReferenceType), type)));
        }

        /// <summary>
        ///   Resolves to direct reference.
        /// </summary>
        /// <returns></returns>
        public abstract DirectReference ResolveToDirectReference(); 
        
        protected abstract object ProvideAdditionalEqualityComponent();

        public override bool Equals(object obj)
        {
            return Equals(obj as Reference);
        }

        public bool Equals(Reference other)
        {
            return equalityHelper.Equals(this, other);
        }
        
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
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