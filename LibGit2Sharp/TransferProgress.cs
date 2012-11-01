using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Expose progress values from a fetch operation.
    /// </summary>
    public class TransferProgress
    {
        private GitTransferProgress gitTransferProgress;

        /// <summary>
        ///   Empty constructor.
        /// </summary>
        protected TransferProgress()
        { }

        /// <summary>
        ///   Constructor.
        /// </summary>
        internal TransferProgress(GitTransferProgress gitTransferProgress)
        {
            this.gitTransferProgress = gitTransferProgress;
        }

        /// <summary>
        ///   Total number of objects.
        /// </summary>
        public virtual int TotalObjects
        {
            get
            {
                return (int) gitTransferProgress.total_objects;
            }
        }

        /// <summary>
        ///   Number of objects indexed.
        /// </summary>
        public virtual int IndexedObjects
        {
            get
            {
                return (int) gitTransferProgress.indexed_objects;
            }
        }

        /// <summary>
        ///   Number of objects received.
        /// </summary>
        public virtual int ReceivedObjects
        {
            get
            {
                return (int) gitTransferProgress.received_objects;
            }
        }

        /// <summary>
        ///   Number of bytes received.
        /// </summary>
        public virtual long ReceivedBytes
        {
            get
            {
                return (long) gitTransferProgress.received_bytes;
            }
        }
    }
}
