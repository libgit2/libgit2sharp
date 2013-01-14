using System;
using System.IO;
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
    ///   The remote branch data that has been fetched from a remote.
    /// </summary>
    public class FetchHeadCollection : IEnumerable<FetchHead>
    {
        internal readonly Repository repo;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected FetchHeadCollection()
        { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="FetchHeadCollection"/> class.
        /// </summary>
        /// <param name="repo">The repo.</param>
        internal FetchHeadCollection(Repository repo)
        {
            this.repo = repo;
        }

        #region IEnumerable<FetchHead> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<FetchHead> GetEnumerator()
        {
            if (File.Exists(Path.Combine(repo.Info.Path, "FETCH_HEAD")))
            {
                return Proxy.git_repository_fetchhead_foreach(repo.Handle,
                    (name, url, oid, is_merge) => new FetchHead(name, url, oid, is_merge))
                    .GetEnumerator();
                }

            return Enumerable.Empty<FetchHead>().GetEnumerator();
        }


        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
