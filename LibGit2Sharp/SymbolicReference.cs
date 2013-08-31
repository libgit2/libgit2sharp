using System.Diagnostics;
using System.Globalization;

namespace LibGit2Sharp
{
    /// <summary>
    /// A SymbolicReference is a reference that points to another reference
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class SymbolicReference : Reference
    {
        private readonly Reference target;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected SymbolicReference()
        { }

        internal SymbolicReference(string canonicalName, string targetIdentifier, Reference target)
            : base(canonicalName, targetIdentifier)
        {
            this.target = target;
        }

        /// <summary>
        /// Gets the target of this <see cref="SymbolicReference"/>
        /// </summary>
        public virtual Reference Target
        {
            get { return target; }
        }

        /// <summary>
        /// Recursively peels the target of the reference until a direct reference is encountered.
        /// </summary>
        /// <returns>The <see cref="DirectReference"/> this <see cref="SymbolicReference"/> points to.</returns>
        public override DirectReference ResolveToDirectReference()
        {
            return (Target == null) ? null : Target.ResolveToDirectReference();
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} => {1} => \"{2}\"",
                    CanonicalName, TargetIdentifier,
                    (Target != null) ? Target.TargetIdentifier : "?");
            }
        }
    }
}
