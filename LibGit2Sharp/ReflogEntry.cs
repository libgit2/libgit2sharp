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
        /// <summary>
        /// <see cref="ObjectId"/> targeted before the reference update described by this <see cref="ReflogEntry"/>
        /// </summary>
        public ObjectId From { get; set; }

        /// <summary>
        /// <see cref="ObjectId"/> targeted after the reference update described by this <see cref="ReflogEntry"/>
        /// </summary>
        public ObjectId To { get; set; }

        /// <summary>
        /// <see cref="Signature"/> of the committer of this reference update
        /// </summary>
        public Signature Committer { get; set; }

        /// <summary>
        /// the message assiocated to this reference update
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflogEntry"/> class.
        /// </summary>
        /// <param name="entryHandle">a <see cref="SafeHandle"/> to the reflog entry</param>
        public ReflogEntry(SafeHandle entryHandle)
        {
            From = Proxy.git_reflog_entry_id_old(entryHandle);
            To = Proxy.git_reflog_entry_id_new(entryHandle);
            Committer = Proxy.git_reflog_entry_committer(entryHandle);
            Message = Proxy.git_reflog_entry_message(entryHandle);
        }
    }
}
