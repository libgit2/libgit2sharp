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
        /// Recursively peels the target of the reference until a direct reference is encountered.
        /// </summary>
        /// <returns></returns>
        public abstract DirectReference ResolveToDirectReference(); 
        
        protected abstract object ProvideAdditionalEqualityComponent();

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Reference"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Reference"/>.</param>
        /// <returns>True if the specified <see cref="Object"/> is equal to the current <see cref="Reference"/>; otherwise, false.</returns>
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
    }
}