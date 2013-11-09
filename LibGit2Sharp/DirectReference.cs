using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp
{
    /// <summary>
    /// A DirectReference points directly to a <see cref="GitObject"/>
    /// </summary>
    public class DirectReference : Reference
    {
        private readonly Lazy<GitObject> targetBuilder;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected DirectReference()
        { }

        internal DirectReference(string canonicalName, IRepository repo, ObjectId targetId)
            : base(canonicalName, targetId.Sha)
        {
            targetBuilder = new Lazy<GitObject>(() => repo.Lookup(targetId));
        }

        /// <summary>
        /// Gets the target of this <see cref="DirectReference"/>
        /// </summary>
        public virtual GitObject Target
        {
            get { return targetBuilder.Value; }
        }

        /// <summary>
        /// As a <see cref="DirectReference"/> is already peeled, invoking this will return the same <see cref="DirectReference"/>.
        /// </summary>
        /// <returns>This instance.</returns>
        public override DirectReference ResolveToDirectReference()
        {
            return this;
        }
    }
}
