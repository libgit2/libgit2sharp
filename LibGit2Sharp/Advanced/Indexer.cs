using System;
using System.IO;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Advanced
{
    public class Indexer
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
            ObjectDatabaseSafeHandle odbHandle = odb != null ? odb.Handle : null;
            handle = Proxy.git_indexer_new(prefix, odbHandle, mode, onProgress);
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
    }
}
