using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Represents .git/FETCH_HEADS (one reference per row)
    /// </summary>
    public class FetchHead
    {
        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected FetchHead()
        { }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "FetchHead" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        internal FetchHead(string name, string url, GitOid oid, bool is_merge)
        {
            Name = name;
            Url = url;
            CommitId = new ObjectId(oid);
            IsMerge = is_merge;
        }

        /// <summary>
        ///   The name of the remote branch.
        /// </summary>
        public virtual String Name { get; private set; }

        /// <summary>
        ///   The URL of the remote branch.
        /// </summary>
        public virtual String Url { get; private set; }

        /// <summary>
        ///   The <see cref = "ObjectId"/> of the remote head.
        /// </summary>
        public virtual ObjectId CommitId { get; private set; }

        /// <summary>
        ///   Determines if this fetch head entry is for merge.
        /// </summary>
        public virtual bool IsMerge { get; private set; }
    }
}