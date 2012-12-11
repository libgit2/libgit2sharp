using System;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A base class for things that wrap a <see cref = "Reference" /> (branch, tag, etc).
    /// </summary>
    /// <typeparam name="TObject">The type of the referenced Git object.</typeparam>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class ReferenceWrapper<TObject> : IEquatable<ReferenceWrapper<TObject>> where TObject : GitObject
    {
        /// <summary>
        ///   The repository.
        /// </summary>
        protected readonly Repository repo;
        private readonly Lazy<TObject> objectBuilder;

        private static readonly LambdaEqualityHelper<ReferenceWrapper<TObject>> equalityHelper =
            new LambdaEqualityHelper<ReferenceWrapper<TObject>>(x => x.CanonicalName, x => x.TargetObject);

        private readonly string canonicalName;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected ReferenceWrapper()
        { }

        /// <param name="repo">The repository.</param>
        /// <param name="reference">The reference.</param>
        /// <param name="canonicalNameSelector">A function to construct the reference's canonical name.</param>
        protected internal ReferenceWrapper(Repository repo, Reference reference, Func<Reference, string> canonicalNameSelector)
        {
            Ensure.ArgumentNotNull(repo, "repo");
            Ensure.ArgumentNotNull(reference, "reference");
            Ensure.ArgumentNotNull(canonicalNameSelector, "canonicalNameSelector");

            this.repo = repo;
            canonicalName = canonicalNameSelector(reference);
            objectBuilder = new Lazy<TObject>(() => RetrieveTargetObject(reference));
        }

        /// <summary>
        ///   Gets the full name of this reference.
        /// </summary>
        public virtual string CanonicalName
        {
            get { return canonicalName; }
        }

        /// <summary>
        ///   Gets the name of this reference.
        /// </summary>
        public virtual string Name
        {
            get { return Shorten(); }
        }

        /// <summary>
        ///   Returns the <see cref = "CanonicalName" />, a <see cref = "string" /> representation of the current reference.
        /// </summary>
        /// <returns>The <see cref = "CanonicalName" /> that represents the current reference.</returns>
        public override string ToString()
        {
            return CanonicalName;
        }

        /// <summary>
        ///   Gets the <typeparamref name="TObject"/> this <see cref = "ReferenceWrapper{TObject}" /> points to.
        /// </summary>
        protected TObject TargetObject
        {
            get { return objectBuilder.Value; }
        }

        /// <summary>
        ///   Removes redundent leading namespaces (regarding the kind of
        ///   reference being wrapped) from the canonical name.
        /// </summary>
        /// <returns>The friendly shortened name</returns>
        protected abstract string Shorten();

        private TObject RetrieveTargetObject(Reference reference)
        {
            var directReference = reference.ResolveToDirectReference();
            if (directReference == null)
            {
                return null;
            }

            var target = directReference.Target;
            if (target == null)
            {
                return null;
            }

            return repo.Lookup<TObject>(target.Id);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "ReferenceWrapper{TObject}" /> is equal to the current <see cref = "ReferenceWrapper{TObject}" />.
        /// </summary>
        /// <param name = "other">The <see cref = "ReferenceWrapper{TObject}" /> to compare with the current <see cref = "ReferenceWrapper{TObject}" />.</param>
        /// <returns>True if the specified <see cref = "ReferenceWrapper{TObject}" /> is equal to the current <see cref = "ReferenceWrapper{TObject}" />; otherwise, false.</returns>
        public bool Equals(ReferenceWrapper<TObject> other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "Object" /> is equal to the current <see cref = "ReferenceWrapper{TObject}" />.
        /// </summary>
        /// <param name = "obj">The <see cref = "Object" /> to compare with the current <see cref = "ReferenceWrapper{TObject}" />.</param>
        /// <returns>True if the specified <see cref = "Object" /> is equal to the current <see cref = "ReferenceWrapper{TObject}" />; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ReferenceWrapper<TObject>);
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
        ///   Tests if two <see cref = "ReferenceWrapper{TObject}" /> are equal.
        /// </summary>
        /// <param name = "left">First <see cref = "ReferenceWrapper{TObject}" /> to compare.</param>
        /// <param name = "right">Second <see cref = "ReferenceWrapper{TObject}" /> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(ReferenceWrapper<TObject> left, ReferenceWrapper<TObject> right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Tests if two <see cref = "ReferenceWrapper{TObject}" /> are different.
        /// </summary>
        /// <param name = "left">First <see cref = "ReferenceWrapper{TObject}" /> to compare.</param>
        /// <param name = "right">Second <see cref = "ReferenceWrapper{TObject}" /> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(ReferenceWrapper<TObject> left, ReferenceWrapper<TObject> right)
        {
            return !Equals(left, right);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} => \"{1}\"", CanonicalName,
                    (TargetObject != null) ? TargetObject.Id.ToString(7) : "?");
            }
        }
    }
}
