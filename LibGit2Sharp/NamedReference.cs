using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public abstract class NamedReference<TObject> where TObject : GitObject
    {
        protected readonly Repository repo;
        private readonly Lazy<TObject> objectBuilder;

        protected internal NamedReference(Repository repo, Reference reference, Func<Reference, string> canonicalNameSelector)
        {
            Ensure.ArgumentNotNull(repo, "repo");
            Ensure.ArgumentNotNull(canonicalNameSelector, "canonicalNameSelector");

            this.repo = repo;
            CanonicalName = canonicalNameSelector(reference);
            objectBuilder = new Lazy<TObject>(() => GetObject(reference));
        }

        /// <summary>
        ///   Gets the full name of this reference.
        /// </summary>
        public string CanonicalName { get; protected set; }

        /// <summary>
        ///   Gets the name of this reference.
        /// </summary>
        public string Name
        {
            get { return Shorten(CanonicalName); }
        }

        /// <summary>
        ///   Returns the <see cref = "CanonicalName" />, a <see cref = "string" /> representation of the current reference.
        /// </summary>
        /// <returns>The <see cref = "CanonicalName" /> that represents the current reference.</returns>
        public override string ToString()
        {
            return CanonicalName;
        }

        protected TObject Object { get { return objectBuilder.Value; } }

        protected abstract string Shorten(string tagName);

        private TObject GetObject(Reference reference)
        {
            Ensure.ArgumentNotNull(reference, "reference");

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
    }
}