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
        // This is here to keep the pointer alive
#pragma warning disable 0414
        readonly Remote remote;
#pragma warning restore 0414
        readonly IntPtr handle;

        internal unsafe RefSpec(Remote remote, git_refspec* handle)
        {
            this.remote = remote;
            this.handle = new IntPtr(handle);
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

        /// <summary>
        /// Check whether the given reference matches the source (lhs) part of
        /// this refspec.
        /// </summary>
        /// <param name="reference">The reference name to check</param>
        public virtual bool SourceMatches(string reference)
        {
            return Proxy.git_refspec_src_matches(handle, reference);
        }

        /// <summary>
        /// Check whether the given reference matches the target (rhs) part of
        /// this refspec.
        /// </summary>
        /// <param name="reference">The reference name to check</param>
        public virtual bool DestinationMatches(string reference)
        {
            return Proxy.git_refspec_dst_matches(handle, reference);
        }

        /// <summary>
        /// Perform the transformation described by this refspec on the given
        /// reference name (from source to destination).
        /// </summary>
        /// <param name="reference">The reference name to transform</param>
        public virtual string Transform(string reference)
        {
            return Proxy.git_refspec_transform(handle, reference);
        }

        /// <summary>
        /// Perform the reverse of the transformation described by this refspec
        /// on the given reference name (from destination to source).
        /// </summary>
        /// <param name="reference">The reference name to transform</param>
        public virtual string ReverseTransform(string reference)
        {
            return Proxy.git_refspec_rtransform(handle, reference);
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
