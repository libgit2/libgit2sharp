using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A DirectReference points directly to a <see cref = "GitObject" />
    /// </summary>
    public class DirectReference : Reference
    {
        private readonly Lazy<IGitObject> targetBuilder;

        internal DirectReference(Lazy<IGitObject> targetBuilder)
        {
            this.targetBuilder = targetBuilder;
        }

        /// <summary>
        ///   Gets the target of this <see cref = "DirectReference" />
        /// </summary>
        public IGitObject Target
        {
            get { return targetBuilder.Value; }
        }

        /// <summary>
        ///   As a <see cref = "DirectReference" /> is already peeled, invoking this will return the same <see cref = "DirectReference" />.
        /// </summary>
        /// <returns>This instance.</returns>
        public override DirectReference ResolveToDirectReference()
        {
            return this;
        }
    }
}
