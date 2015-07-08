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
        readonly IList<RefSpec> refspecs;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected RefSpecCollection()
        { }

        internal RefSpecCollection(RemoteSafeHandle handle)
        {
            Ensure.ArgumentNotNull(handle, "handle");

            refspecs = RetrieveRefSpecs(handle);
        }

        static IList<RefSpec> RetrieveRefSpecs(RemoteSafeHandle remoteHandle)
        {
            int count = Proxy.git_remote_refspec_count(remoteHandle);
            List<RefSpec> refSpecs = new List<RefSpec>();

            for (int i = 0; i < count; i++)
            {
                using (GitRefSpecHandle handle = Proxy.git_remote_get_refspec(remoteHandle, i))
                {
                    refSpecs.Add(RefSpec.BuildFromPtr(handle));
                }
            }

            return refSpecs;

        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<RefSpec> GetEnumerator()
        {
            return refspecs.GetEnumerator();
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
