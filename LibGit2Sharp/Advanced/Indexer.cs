using System;
using System.IO;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Advanced
{
    /// <summary>
    /// The Indexer is our implementation of the git-index-pack command. It is used to process the packfile
    /// which comes from the remote side on a fetch in order to create the corresponding .idx file.
    /// </summary>
    public class Indexer : IDisposable
    {

        readonly IndexerSafeHandle handle;
        readonly TransferProgressHandler callback;

        Indexer(string prefix, uint mode, ObjectDatabase odb = null, TransferProgressHandler onProgress = null)
        {
            /* The runtime won't let us pass null as a SafeHandle, wo create a "dummy" one to represent NULL */
            ObjectDatabaseSafeHandle odbHandle = odb != null ? odb.Handle : new ObjectDatabaseSafeHandle();
            callback = onProgress;
            handle = Proxy.git_indexer_new(prefix, odbHandle, mode, GitDownloadTransferProgressHandler);
        }

        /// <summary>
        /// Index the specified stream. This function runs synchronously; you may want to run it
        /// in a background thread.
        /// </summary>
        /// <param name="progress">The amount of objects processed etc will be written to this structure on exit</param>
        /// <param name="stream">Stream to run the indexing process on</param>
        /// <param name="prefix">Path in which to store the pack and index files</param>
        /// <param name="mode">Filemode to use for creating the pack and index files</param>
        /// <param name="odb">Optional object db to use if the pack contains thin deltas</param>
        /// <param name="onProgress">Function to call to report progress. It returns a boolean indicating whether
        /// to continue working on the stream</param>
        public static ObjectId Index(out TransferProgress progress, Stream stream, string prefix, uint mode, ObjectDatabase odb = null, TransferProgressHandler onProgress = null)
        {
            var buffer = new byte[65536];
            int read;
            var indexProgress = default(GitTransferProgress);

            using (var idx = new Indexer(prefix, mode, odb, onProgress))
            {
                var handle = idx.handle;

                do
                {
                    read = stream.Read(buffer, 0, buffer.Length);
                    Proxy.git_indexer_append(handle, buffer, (UIntPtr)read, ref indexProgress);
                } while (read > 0);

                Proxy.git_indexer_commit(handle, ref indexProgress);

                progress = new TransferProgress(indexProgress);
                return Proxy.git_indexer_hash(handle);
            }
        }

        /// <summary>
        /// Index the packfile at the specified path. This function runs synchronously; you may want to run it
        /// in a background thread.
        /// <param name="progress">The amount of objects processed etc will be written to this structure on exit</param>
        /// <param name="path">Path to the file to index</param>
        /// <param name="prefix">Path in which to store the pack and index files</param>
        /// <param name="mode">Filemode to use for creating the pack and index files</param>
        /// <param name="odb">Optional object db to use if the pack contains thin deltas</param>
        /// <param name="onProgress">Function to call to report progress. It returns a boolean indicating whether
        /// to continue working on the stream</param>

        /// </summary>
        public static ObjectId Index(out TransferProgress progress, string path, string prefix, uint mode, ObjectDatabase odb = null, TransferProgressHandler onProgress = null)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return Index(out progress, fs, prefix, mode, odb, onProgress);
            }
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
