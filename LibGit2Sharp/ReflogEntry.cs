using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// As single entry of a <see cref="ReflogCollection"/>
    /// a <see cref="ReflogEntry"/> describes one single update on a particular reference
    /// </summary>
    public class ReflogEntry
    {
        private readonly ObjectId _from;
        private readonly ObjectId _to;
        private readonly Signature _committer;
        private readonly string message;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected ReflogEntry()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflogEntry"/> class.
        /// </summary>
        /// <param name="entryHandle">a <see cref="SafeHandle"/> to the reflog entry</param>
        public ReflogEntry(SafeHandle entryHandle)
        {
            _from = Proxy.git_reflog_entry_id_old(entryHandle);
            _to = Proxy.git_reflog_entry_id_new(entryHandle);
            _committer = Proxy.git_reflog_entry_committer(entryHandle);
            message = Proxy.git_reflog_entry_message(entryHandle);
        }

        /// <summary>
        /// <see cref="ObjectId"/> targeted before the reference update described by this <see cref="ReflogEntry"/>
        /// </summary>
        public virtual ObjectId From
        {
            get { return _from; }
        }

        /// <summary>
        /// <see cref="ObjectId"/> targeted after the reference update described by this <see cref="ReflogEntry"/>
        /// </summary>
        public virtual ObjectId To
        {
            get { return _to; }
        }

        /// <summary>
        /// <see cref="Signature"/> of the committer of this reference update
        /// </summary>
        public virtual Signature Committer
        {
            get { return _committer; }
        }

        /// <summary>
        /// <see cref="Signature"/> of the committer of this reference update
        /// </summary>
        [Obsolete("This property will be removed in the next release. Please use Committer instead.")]
        public virtual Signature Commiter
        {
            get { return Committer; }
        }

        /// <summary>
        /// the message assiocated to this reference update
        /// </summary>
        public virtual string Message
        {
            get { return message; }
        }
    }
}
