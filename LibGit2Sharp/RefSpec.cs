using System;
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
        readonly Remote remote;
        readonly GitRefSpecHandle handle;

        internal RefSpec(Remote remote, GitRefSpecHandle handle)
        {
            this.remote = remote;
            this.handle = handle;
        }

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected RefSpec()
        { }

        /// <summary>
        /// Gets the pattern describing the mapping between remote and local references
        /// </summary>
        public virtual string Specification
        {
            get
            {
                return Proxy.git_refspec_string(this.handle);
            }
        }

        /// <summary>
        /// Indicates whether this <see cref="RefSpec"/> is intended to be used during a Push or Fetch operation
        /// </summary>
        public virtual RefSpecDirection Direction
        {
            get
            {
                return Proxy.git_refspec_direction(this.handle);
            }
        }

        /// <summary>
        /// The source reference specifier
        /// </summary>
        public virtual string Source
        {
            get
            {
                return Proxy.git_refspec_src(this.handle);
            }
        }

        /// <summary>
        /// The target reference specifier
        /// </summary>
        public virtual string Destination
        {
            get
            {
                return Proxy.git_refspec_dst(this.handle);
            }
        }

        /// <summary>
        /// Indicates whether the destination will be force-updated if fast-forwarding is not possible
        /// </summary>
        public virtual bool ForceUpdate
        {
            get
            {
                return Proxy.git_refspec_force(this.handle);
            }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}", Specification);
            }
        }
    }
}
