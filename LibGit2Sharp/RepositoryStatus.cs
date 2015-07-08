using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Holds the result of the determination of the state of the working directory.
    /// <para>Only files that differ from the current index and/or commit will be considered.</para>
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class RepositoryStatus : IEnumerable<StatusEntry>
    {
        private readonly ICollection<StatusEntry> statusEntries;
        private readonly List<StatusEntry> added = new List<StatusEntry>();
        private readonly List<StatusEntry> staged = new List<StatusEntry>();
        private readonly List<StatusEntry> removed = new List<StatusEntry>();
        private readonly List<StatusEntry> missing = new List<StatusEntry>();
        private readonly List<StatusEntry> modified = new List<StatusEntry>();
        private readonly List<StatusEntry> untracked = new List<StatusEntry>();
        private readonly List<StatusEntry> ignored = new List<StatusEntry>();
        private readonly List<StatusEntry> renamedInIndex = new List<StatusEntry>();
        private readonly List<StatusEntry> renamedInWorkDir = new List<StatusEntry>();
        private readonly List<StatusEntry> unaltered = new List<StatusEntry>();
        private readonly bool isDirty;

        private readonly IDictionary<FileStatus, Action<RepositoryStatus, StatusEntry>> dispatcher = Build();

        private static IDictionary<FileStatus, Action<RepositoryStatus, StatusEntry>> Build()
        {
            return new Dictionary<FileStatus, Action<RepositoryStatus, StatusEntry>>
            {
                { FileStatus.NewInWorkdir, (rs, s) => rs.untracked.Add(s) },
                { FileStatus.ModifiedInWorkdir, (rs, s) => rs.modified.Add(s) },
                { FileStatus.DeletedFromWorkdir, (rs, s) => rs.missing.Add(s) },
                { FileStatus.NewInIndex, (rs, s) => rs.added.Add(s) },
                { FileStatus.ModifiedInIndex, (rs, s) => rs.staged.Add(s) },
                { FileStatus.DeletedFromIndex, (rs, s) => rs.removed.Add(s) },
                { FileStatus.RenamedInIndex, (rs, s) => rs.renamedInIndex.Add(s) },
                { FileStatus.Ignored, (rs, s) => rs.ignored.Add(s) },
                { FileStatus.RenamedInWorkdir, (rs, s) => rs.renamedInWorkDir.Add(s) },
            };
        }

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected RepositoryStatus()
        { }

        internal RepositoryStatus(Repository repo, StatusOptions options)
        {
            statusEntries = new List<StatusEntry>();

            using (GitStatusOptions coreOptions = CreateStatusOptions(options ?? new StatusOptions()))
            using (StatusListSafeHandle list = Proxy.git_status_list_new(repo.Handle, coreOptions))
            {
                int count = Proxy.git_status_list_entrycount(list);

                for (int i = 0; i < count; i++)
                {
                    StatusEntrySafeHandle e = Proxy.git_status_byindex(list, i);
                    GitStatusEntry entry = e.MarshalAsGitStatusEntry();

                    GitDiffDelta deltaHeadToIndex = null;
                    GitDiffDelta deltaIndexToWorkDir = null;

                    if (entry.HeadToIndexPtr != IntPtr.Zero)
                    {
                        deltaHeadToIndex = entry.HeadToIndexPtr.MarshalAs<GitDiffDelta>();
                    }
                    if (entry.IndexToWorkDirPtr != IntPtr.Zero)
                    {
                        deltaIndexToWorkDir = entry.IndexToWorkDirPtr.MarshalAs<GitDiffDelta>();
                    }

                    AddStatusEntryForDelta(entry.Status, deltaHeadToIndex, deltaIndexToWorkDir);
                }

                isDirty = statusEntries.Any(entry => entry.State != FileStatus.Ignored && entry.State != FileStatus.Unaltered);
            }
        }

        private static GitStatusOptions CreateStatusOptions(StatusOptions options)
        {
            var coreOptions = new GitStatusOptions
            {
                Version = 1,
                Show = (GitStatusShow)options.Show,
                Flags =
                    GitStatusOptionFlags.IncludeIgnored |
                    GitStatusOptionFlags.IncludeUntracked |
                    GitStatusOptionFlags.RecurseUntrackedDirs,
            };

            if (options.DetectRenamesInIndex)
            {
                coreOptions.Flags |=
                    GitStatusOptionFlags.RenamesHeadToIndex |
                    GitStatusOptionFlags.RenamesFromRewrites;
            }

            if (options.DetectRenamesInWorkDir)
            {
                coreOptions.Flags |=
                    GitStatusOptionFlags.RenamesIndexToWorkDir |
                    GitStatusOptionFlags.RenamesFromRewrites;
            }

            if (options.ExcludeSubmodules)
            {
                coreOptions.Flags |=
                    GitStatusOptionFlags.ExcludeSubmodules;
            }

            if (options.RecurseIgnoredDirs)
            {
                coreOptions.Flags |=
                    GitStatusOptionFlags.RecurseIgnoredDirs;
            }

            if (options.PathSpec != null)
            {
                coreOptions.PathSpec = GitStrArrayManaged.BuildFrom(options.PathSpec);
            }

            if (options.DisablePathSpecMatch)
            {
                coreOptions.Flags |=
                    GitStatusOptionFlags.DisablePathspecMatch;
            }

            if (options.IncludeUnaltered)
            {
                coreOptions.Flags |=
                    GitStatusOptionFlags.IncludeUnmodified;
            }

            return coreOptions;
        }

        private void AddStatusEntryForDelta(FileStatus gitStatus, GitDiffDelta deltaHeadToIndex, GitDiffDelta deltaIndexToWorkDir)
        {
            RenameDetails headToIndexRenameDetails = null;
            RenameDetails indexToWorkDirRenameDetails = null;

            if ((gitStatus & FileStatus.RenamedInIndex) == FileStatus.RenamedInIndex)
            {
                headToIndexRenameDetails =
                    new RenameDetails(LaxFilePathMarshaler.FromNative(deltaHeadToIndex.OldFile.Path).Native,
                                      LaxFilePathMarshaler.FromNative(deltaHeadToIndex.NewFile.Path).Native,
                                      (int)deltaHeadToIndex.Similarity);
            }

            if ((gitStatus & FileStatus.RenamedInWorkdir) == FileStatus.RenamedInWorkdir)
            {
                indexToWorkDirRenameDetails =
                    new RenameDetails(LaxFilePathMarshaler.FromNative(deltaIndexToWorkDir.OldFile.Path).Native,
                                      LaxFilePathMarshaler.FromNative(deltaIndexToWorkDir.NewFile.Path).Native,
                                      (int)deltaIndexToWorkDir.Similarity);
            }

            var filePath = (deltaIndexToWorkDir != null)
                ? LaxFilePathMarshaler.FromNative(deltaIndexToWorkDir.NewFile.Path).Native
                : LaxFilePathMarshaler.FromNative(deltaHeadToIndex.NewFile.Path).Native;

            StatusEntry statusEntry = new StatusEntry(filePath, gitStatus, headToIndexRenameDetails, indexToWorkDirRenameDetails);

            if (gitStatus == FileStatus.Unaltered)
            {
                unaltered.Add(statusEntry);
            }
            else
            {
                foreach (KeyValuePair<FileStatus, Action<RepositoryStatus, StatusEntry>> kvp in dispatcher)
                {
                    if (!gitStatus.HasFlag(kvp.Key))
                    {
                        continue;
                    }

                    kvp.Value(this, statusEntry);
                }
            }

            statusEntries.Add(statusEntry);
        }

        /// <summary>
        /// Gets the <see cref="StatusEntry"/> for the specified relative path.
        /// </summary>
        public virtual StatusEntry this[string path]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(path, "path");

                var entries = statusEntries.Where(e => string.Equals(e.FilePath, path, StringComparison.Ordinal)).ToList();

                Debug.Assert(!(entries.Count > 1));

                if (entries.Count == 0)
                {
                    return new StatusEntry(path, FileStatus.Nonexistent);
                }

                return entries.Single();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<StatusEntry> GetEnumerator()
        {
            return statusEntries.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// List of files added to the index, which are not in the current commit
        /// </summary>
        public virtual IEnumerable<StatusEntry> Added
        {
            get { return added; }
        }

        /// <summary>
        /// List of files added to the index, which are already in the current commit with different content
        /// </summary>
        public virtual IEnumerable<StatusEntry> Staged
        {
            get { return staged; }
        }

        /// <summary>
        /// List of files removed from the index but are existent in the current commit
        /// </summary>
        public virtual IEnumerable<StatusEntry> Removed
        {
            get { return removed; }
        }

        /// <summary>
        /// List of files existent in the index but are missing in the working directory
        /// </summary>
        public virtual IEnumerable<StatusEntry> Missing
        {
            get { return missing; }
        }

        /// <summary>
        /// List of files with unstaged modifications. A file may be modified and staged at the same time if it has been modified after adding.
        /// </summary>
        public virtual IEnumerable<StatusEntry> Modified
        {
            get { return modified; }
        }

        /// <summary>
        /// List of files existing in the working directory but are neither tracked in the index nor in the current commit.
        /// </summary>
        public virtual IEnumerable<StatusEntry> Untracked
        {
            get { return untracked; }
        }

        /// <summary>
        /// List of files existing in the working directory that are ignored.
        /// </summary>
        public virtual IEnumerable<StatusEntry> Ignored
        {
            get { return ignored; }
        }

        /// <summary>
        /// List of files that were renamed and staged.
        /// </summary>
        public virtual IEnumerable<StatusEntry> RenamedInIndex
        {
            get { return renamedInIndex; }
        }

        /// <summary>
        /// List of files that were renamed in the working directory but have not been staged.
        /// </summary>
        public virtual IEnumerable<StatusEntry> RenamedInWorkDir
        {
            get { return renamedInWorkDir; }
        }

        /// <summary>
        /// List of files that were unmodified in the working directory.
        /// </summary>
        public virtual IEnumerable<StatusEntry> Unaltered
        {
            get { return unaltered; }
        }

        /// <summary>
        /// True if the index or the working directory has been altered since the last commit. False otherwise.
        /// </summary>
        public virtual bool IsDirty
        {
            get { return isDirty; }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "+{0} ~{1} -{2} | +{3} ~{4} -{5} | i{6}",
                                     Added.Count(),
                                     Staged.Count(),
                                     Removed.Count(),
                                     Untracked.Count(),
                                     Modified.Count(),
                                     Missing.Count(),
                                     Ignored.Count());
            }
        }
    }
}
