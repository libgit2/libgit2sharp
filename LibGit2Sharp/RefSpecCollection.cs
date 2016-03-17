using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// The collection of <see cref="RefSpec"/>s in a <see cref="Remote"/>
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class RefSpecCollection : IEnumerable<RefSpec>
    {
        // These are here to keep the pointer alive
        #pragma warning disable 0414
        readonly Remote remote;
        readonly RemoteSafeHandle handle;
        #pragma warning restore 0414
        readonly Lazy<IList<RefSpec>> refspecs;

        internal RefSpecCollection(Remote remote, RemoteSafeHandle handle)
        {
            Ensure.ArgumentNotNull(handle, "handle");

            this.remote = remote;
            this.handle = handle;

            refspecs = new Lazy<IList<RefSpec>>(() => RetrieveRefSpecs(remote, handle));
        }

        static IList<RefSpec> RetrieveRefSpecs(Remote remote, RemoteSafeHandle remoteHandle)
        {
            int count = Proxy.git_remote_refspec_count(remoteHandle);
            List<RefSpec> refSpecs = new List<RefSpec>();

            for (int i = 0; i < count; i++)
            {
                refSpecs.Add(new RefSpec(remote, Proxy.git_remote_get_refspec(remoteHandle, i)));
            }

            return refSpecs;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<RefSpec> GetEnumerator()
        {
            return refspecs.Value.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "Count = {0}", this.Count());
            }
        }
    }
}
