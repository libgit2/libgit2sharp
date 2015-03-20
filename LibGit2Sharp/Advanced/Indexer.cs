using System;
using System.IO;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Advanced
{
    public class Indexer : IDisposable
    {

        readonly IndexerSafeHandle handle;
        readonly TransferProgressHandler callback;

        GitTransferProgress progress;
        byte[] buffer;

        /// <summary>
        /// The indexing progress
        /// </summary>
        /// <value>The progres information for the current operation.</value>
        public TransferProgress Progress
        {
            get
            {
                return new TransferProgress(progress);
            }
        }

        public Indexer(string prefix, uint mode, ObjectDatabase odb = null, TransferProgressHandler onProgress = null)
        {
            /* The runtime won't let us pass null as a SafeHandle, wo create a "dummy" one to represent NULL */
            ObjectDatabaseSafeHandle odbHandle = odb != null ? odb.Handle : new ObjectDatabaseSafeHandle();
            callback = onProgress;
            handle = Proxy.git_indexer_new(prefix, odbHandle, mode, GitDownloadTransferProgressHandler);
            progress = new GitTransferProgress();
        }

        /// <summary>
        /// Index the packfile at the specified path. This function runs synchronously and should usually be run
        /// in a background thread.
        /// </summary>
        /// <param name="path">The packfile's path</param>
        public ObjectId Index(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return Index(fs);
            }
        }

        /// <summary>
        /// Index the packfile from the specified stream. This function runs synchronously and should usually be run
        /// in a background thread.
        /// </summary>
        /// <param name="stream">The stream from which to read the packfile data</param>
        public ObjectId Index(Stream stream)
        {
            buffer = new byte[65536];
            int read;

            do
            {
                read = stream.Read(buffer, 0, buffer.Length);
                Proxy.git_indexer_append(handle, buffer, (UIntPtr)read, ref progress);
            } while (read > 0);

            Proxy.git_indexer_commit(handle, ref progress);

            return Proxy.git_indexer_hash(handle);
        }

        // This comes from RemoteCallbacks
        /// <summary>
        /// The delegate with the signature that matches the native git_transfer_progress_callback function's signature.
        /// </summary>
        /// <param name="progress"><see cref="GitTransferProgress"/> structure containing progress information.</param>
        /// <param name="payload">Payload data.</param>
        /// <returns>the result of the wrapped <see cref="TransferProgressHandler"/></returns>
        int GitDownloadTransferProgressHandler(ref GitTransferProgress progress, IntPtr payload)
        {
            bool shouldContinue = true;

            if (callback != null)
            {
                shouldContinue = callback(new TransferProgress(progress));
            }

            return Proxy.ConvertResultToCancelFlag(shouldContinue);
        }

        #region IDisposable implementation

        public void Dispose()
        {
            handle.SafeDispose();
        }

        #endregion
    }
}
