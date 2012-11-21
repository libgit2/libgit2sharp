using System.Diagnostics;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A SymbolicReference is a reference that points to another reference
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class SymbolicReference : Reference
    {
        /// <summary>
        ///   Gets the target of this <see cref = "SymbolicReference" />
        /// </summary>
        public Reference Target { get; internal set; }

        /// <summary>
        ///   Recursively peels the target of the reference until a direct reference is encountered.
        /// </summary>
        /// <returns>The <see cref = "DirectReference" /> this <see cref = "SymbolicReference" /> points to.</returns>
        public override DirectReference ResolveToDirectReference()
        {
            return (Target == null) ? null : Target.ResolveToDirectReference();
        }

        private string DebuggerDisplay
        {
            get
            {
                var directReference = ResolveToDirectReference();
                return string.Format("{0} => {1} => \"{2}\"",
                    CanonicalName, TargetIdentifier,
                    (directReference != null) ? directReference.TargetIdentifier : "?");
            }
        }
    }
}
