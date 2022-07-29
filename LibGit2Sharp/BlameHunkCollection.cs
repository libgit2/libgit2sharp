using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// The result of a blame operation.
    /// </summary>
    public class BlameHunkCollection : IEnumerable<BlameHunk>, IDisposable
    {
        private readonly IRepository repo;
        private readonly List<BlameHunk> hunks = new List<BlameHunk>();
        private readonly RepositoryHandle repoHandle;
        private readonly BlameHandle blameHandle;

        /// <summary>
        /// For easy mocking
        /// </summary>
        protected BlameHunkCollection() { }

        internal unsafe BlameHunkCollection(Repository repo, RepositoryHandle repoHandle, string path, BlameOptions options)
        {
            this.repo = repo;

            var rawopts = new git_blame_options
            {
                version = 1,
                flags = options.Strategy.ToGitBlameOptionFlags(),
                min_line = new UIntPtr((uint)options.MinLine),
                max_line = new UIntPtr((uint)options.MaxLine),
            };

            if (options.StartingAt != null)
            {
                fixed (byte* p = rawopts.newest_commit.Id)
                {
                    Marshal.Copy(repo.Committish(options.StartingAt).Oid.Id, 0, new IntPtr(p), git_oid.Size);
                }
            }

            if (options.StoppingAt != null)
            {
                fixed (byte* p = rawopts.oldest_commit.Id)
                {
                    Marshal.Copy(repo.Committish(options.StoppingAt).Oid.Id, 0, new IntPtr(p), git_oid.Size);
                }
            }

            blameHandle = Proxy.git_blame_file(repoHandle, path, rawopts);

            this.PopulateHunks();
        }

        private unsafe BlameHunkCollection(IRepository repo, RepositoryHandle repoHandle, BlameHandle reference, byte[] buffer)
        {
            this.repo = repo;
            this.repoHandle = repoHandle;
            this.blameHandle = Proxy.git_blame_buffer(repoHandle, reference, buffer);

            this.PopulateHunks();
        }

        public BlameHunkCollection FromBuffer(byte[] buffer)
        {
            return new BlameHunkCollection(repo, repoHandle, this.blameHandle, buffer);
        }

        /// <summary>
        /// Access blame hunks by index.
        /// </summary>
        /// <param name="idx">The index of the hunk to retrieve</param>
        /// <returns>The <see cref="BlameHunk"/> at the given index.</returns>
        public virtual BlameHunk this[int idx]
        {
            get { return hunks[idx]; }
        }

        /// <summary>
        /// Access blame hunks by the file line.
        /// </summary>
        /// <param name="line">Line number to search for</param>
        /// <returns>The <see cref="BlameHunk"/> that contains the specified file line.</returns>
        public virtual BlameHunk HunkForLine(int line)
        {
            var hunk = hunks.FirstOrDefault(x => x.ContainsLine(line));
            if (hunk != null)
            {
                return hunk;
            }
            throw new ArgumentOutOfRangeException("line", "No hunk for that line");
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public virtual IEnumerator<BlameHunk> GetEnumerator()
        {
            return hunks.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private unsafe void PopulateHunks()
        {
            var numHunks = NativeMethods.git_blame_get_hunk_count(blameHandle);
            for (uint i = 0; i < numHunks; ++i)
            {
                var rawHunk = Proxy.git_blame_get_hunk_byindex(blameHandle, i);
                hunks.Add(new BlameHunk(this.repo, rawHunk));
            }
        }

        public void Dispose()
        {
            this.blameHandle.Dispose();
        }
    }
}
