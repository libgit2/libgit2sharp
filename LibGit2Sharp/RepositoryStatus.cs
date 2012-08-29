using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Holds the result of the determination of the state of the working directory.
    ///   <para>Only files that differ from the current index and/or commit will be considered.</para>
    /// </summary>
    public class RepositoryStatus : IEnumerable<StatusEntry>
    {
        private readonly List<StatusEntry> statusEntries = new List<StatusEntry>();
        private readonly List<string> added = new List<string>();
        private readonly List<string> staged = new List<string>();
        private readonly List<string> removed = new List<string>();
        private readonly List<string> missing = new List<string>();
        private readonly List<string> modified = new List<string>();
        private readonly List<string> untracked = new List<string>();
        private readonly List<string> ignored = new List<string>();
        private readonly bool isDirty;

        private readonly IDictionary<FileStatus, Action<RepositoryStatus, string>> dispatcher = Build();

        private static IDictionary<FileStatus, Action<RepositoryStatus, string>> Build()
        {
            return new Dictionary<FileStatus, Action<RepositoryStatus, string>>
                       {
                           { FileStatus.Untracked, (rs, s) => rs.untracked.Add(s) },
                           { FileStatus.Modified, (rs, s) => rs.modified.Add(s) },
                           { FileStatus.Missing, (rs, s) => rs.missing.Add(s) },
                           { FileStatus.Added, (rs, s) => rs.added.Add(s) },
                           { FileStatus.Staged, (rs, s) => rs.staged.Add(s) },
                           { FileStatus.Removed, (rs, s) => rs.removed.Add(s) },
                           { FileStatus.Ignored, (rs, s) => rs.ignored.Add(s) },
                       };
        }

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected RepositoryStatus()
        { }

        internal RepositoryStatus(Repository repo)
        {
            Proxy.git_status_foreach(repo.Handle, StateChanged);
            isDirty = statusEntries.Any(entry => entry.State != FileStatus.Ignored);
        }

        private int StateChanged(IntPtr filePathPtr, uint state, IntPtr payload)
        {
            var filePath = FilePathMarshaler.FromNative(filePathPtr);
            var gitStatus = (FileStatus)state;
            statusEntries.Add(new StatusEntry(filePath.Native, gitStatus));

            foreach (KeyValuePair<FileStatus, Action<RepositoryStatus, string>> kvp in dispatcher)
            {
                if (!gitStatus.Has(kvp.Key))
                {
                    continue;
                }

                kvp.Value(this, filePath.Native);
            }

            return 0;
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<StatusEntry> GetEnumerator()
        {
            return statusEntries.GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///   List of files added to the index, which are not in the current commit
        /// </summary>
        public virtual IEnumerable<string> Added
        {
            get { return added; }
        }

        /// <summary>
        ///   List of files added to the index, which are already in the current commit with different content
        /// </summary>
        public virtual IEnumerable<string> Staged
        {
            get { return staged; }
        }

        /// <summary>
        ///   List of files removed from the index but are existent in the current commit
        /// </summary>
        public virtual IEnumerable<string> Removed
        {
            get { return removed; }
        }

        /// <summary>
        ///   List of files existent in the index but are missing in the working directory
        /// </summary>
        public virtual IEnumerable<string> Missing
        {
            get { return missing; }
        }

        /// <summary>
        ///   List of files with unstaged modifications. A file may be modified and staged at the same time if it has been modified after adding.
        /// </summary>
        public virtual IEnumerable<string> Modified
        {
            get { return modified; }
        }

        /// <summary>
        ///   List of files existing in the working directory but are neither tracked in the index nor in the current commit.
        /// </summary>
        public virtual IEnumerable<string> Untracked
        {
            get { return untracked; }
        }

        /// <summary>
        ///   List of files existing in the working directory that are ignored.
        /// </summary>
        public virtual IEnumerable<string> Ignored
        {
            get { return ignored; }
        }

        /// <summary>
        ///   True if the index or the working directory has been altered since the last commit. False otherwise.
        /// </summary>
        public virtual bool IsDirty
        {
            get { return isDirty; }
        }
    }
}
