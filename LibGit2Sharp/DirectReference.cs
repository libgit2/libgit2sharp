namespace LibGit2Sharp
{
    /// <summary>
    ///   A DirectReference points directly to a <see cref = "GitObject" />
    /// </summary>
    public class DirectReference : Reference
    {
        /// <summary>
        ///   Gets the target of this <see cref = "DirectReference" />
        /// </summary>
        public GitObject Target { get; internal set; }

        protected override object ProvideAdditionalEqualityComponent()
        {
            return Target;
        }

        /// <summary>
        ///   As a <see cref="DirectReference"/> is already peeled, invoking this will return the same <see cref="DirectReference"/>.
        /// </summary>
        /// <returns>This instance.</returns>
        public override DirectReference ResolveToDirectReference()
        {
            return this;
        }
    }
}