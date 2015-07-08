using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A push or fetch reference specification
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class RefSpec
    {
        private RefSpec(string refSpec, RefSpecDirection direction, string source, string destination, bool forceUpdate)
        {
            Ensure.ArgumentNotNullOrEmptyString(refSpec, "refSpec");
            Ensure.ArgumentNotNull(source, "source");
            Ensure.ArgumentNotNull(destination, "destination");

            Specification = refSpec;
            Direction = direction;
            Source = source;
            Destination = destination;
            ForceUpdate = forceUpdate;
        }

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected RefSpec()
        { }

        internal static RefSpec BuildFromPtr(GitRefSpecHandle handle)
        {
            Ensure.ArgumentNotNull(handle, "handle");

            return new RefSpec(Proxy.git_refspec_string(handle), Proxy.git_refspec_direction(handle),
                Proxy.git_refspec_src(handle), Proxy.git_refspec_dst(handle), Proxy.git_refspec_force(handle));
        }

        /// <summary>
        /// Gets the pattern describing the mapping between remote and local references
        /// </summary>
        public virtual string Specification { get; private set; }

        /// <summary>
        /// Indicates whether this <see cref="RefSpec"/> is intended to be used during a Push or Fetch operation
        /// </summary>
        public virtual RefSpecDirection Direction { get; private set; }

        /// <summary>
        /// The source reference specifier
        /// </summary>
        public virtual string Source { get; private set; }

        /// <summary>
        /// The target reference specifier
        /// </summary>
        public virtual string Destination { get; private set; }

        /// <summary>
        /// Indicates whether the destination will be force-updated if fast-forwarding is not possible
        /// </summary>
        public virtual bool ForceUpdate { get; private set; }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", Specification);
            }
        }
    }
}
