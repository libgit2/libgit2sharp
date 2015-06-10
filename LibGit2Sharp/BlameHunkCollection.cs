using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// The result of a blame operation.
    /// </summary>
    public class BlameHunkCollection : IEnumerable<BlameHunk>
    {
        private readonly IRepository repo;
        private readonly List<BlameHunk> hunks = new List<BlameHunk>();

        /// <summary>
        /// For easy mocking
        /// </summary>
        protected BlameHunkCollection() { }

        internal BlameHunkCollection(Repository repo, RepositorySafeHandle repoHandle, string path, BlameOptions options)
        {
            this.repo = repo;

            var rawopts = new GitBlameOptions
            {
                version = 1,
                FindOptions = new GitDiffFindOptions {
                    Version = 1,
                },
                flags = options.Strategy.ToGitBlameOptionFlags(),
                MinLine = (uint)options.MinLine,
                MaxLine = (uint)options.MaxLine,
            };

            if (options.FindNoRenames)
                rawopts.FindOptions.Flags = GitDiffFindFlags.GIT_DIFF_FIND_NO_RENAMES;
            else if (options.FindExactRenames)
                rawopts.FindOptions.Flags = GitDiffFindFlags.GIT_DIFF_FIND_EXACT_MATCH_ONLY;
            else
                rawopts.FindOptions.Flags = GitDiffFindFlags.GIT_DIFF_FIND_RENAMES;

            if (options.StartingAt != null)
            {
                rawopts.NewestCommit = repo.Committish(options.StartingAt).Oid;
            }

            if (options.StoppingAt != null)
            {
                rawopts.OldestCommit = repo.Committish(options.StoppingAt).Oid;
            }

            using (var blameHandle = Proxy.git_blame_file(repoHandle, path, rawopts))
            {
                var numHunks = NativeMethods.git_blame_get_hunk_count(blameHandle);
                for (uint i = 0; i < numHunks; ++i)
                {
                    var rawHunk = Proxy.git_blame_get_hunk_byindex(blameHandle, i);
                    hunks.Add(new BlameHunk(this.repo, rawHunk));
                }
            }
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
    }
}
