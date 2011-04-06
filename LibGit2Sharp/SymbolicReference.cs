namespace LibGit2Sharp
{
    /// <summary>
    ///   A SymbolicReference is a reference that points to another reference
    /// </summary>
    public class SymbolicReference : Reference
    {
        /// <summary>
        ///   Gets the target of this <see cref = "SymbolicReference" />
        /// </summary>
        public Reference Target { get; internal set; }

        protected override object ProvideAdditionalEqualityComponent()
        {
            return Target;
        }
    }
}