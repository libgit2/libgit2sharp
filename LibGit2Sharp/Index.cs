using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// The Index is a staging area between the Working directory and the Repository.
    /// It's used to prepare and aggregate the changes that will be part of the next commit.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Index : IEnumerable<IndexEntry>
    {
        private readonly IndexSafeHandle handle;
        private readonly Repository repo;
        private readonly ConflictCollection conflicts;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Index()
        { }

        internal Index(Repository repo)
        {
            this.repo = repo;

            handle = Proxy.git_repository_index(repo.Handle);
            conflicts = new ConflictCollection(repo);

            repo.RegisterForCleanup(handle);
        }

        internal Index(Repository repo, string indexPath)
        {
            this.repo = repo;

            handle = Proxy.git_index_open(indexPath);
            Proxy.git_repository_set_index(repo.Handle, handle);
            conflicts = new ConflictCollection(repo);

            repo.RegisterForCleanup(handle);
        }

        internal IndexSafeHandle Handle
        {
            get { return handle; }
        }

        /// <summary>
        /// Gets the number of <see cref="IndexEntry"/> in the index.
        /// </summary>
        public virtual int Count
        {
            get { return Proxy.git_index_entrycount(handle); }
        }

        /// <summary>
        /// Determines if the index is free from conflicts.
        /// </summary>
        public virtual bool IsFullyMerged
        {
            get { return !Proxy.git_index_has_conflicts(handle); }
        }

        /// <summary>
        /// Gets the <see cref="IndexEntry"/> with the specified relative path.
        /// </summary>
        public virtual IndexEntry this[string path]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(path, "path");

                IndexEntrySafeHandle entryHandle = Proxy.git_index_get_bypath(handle, path, 0);
                return IndexEntry.BuildFromPtr(entryHandle);
            }
        }

        private IndexEntry this[int index]
        {
            get
            {
                IndexEntrySafeHandle entryHandle = Proxy.git_index_get_byindex(handle, (UIntPtr)index);
                return IndexEntry.BuildFromPtr(entryHandle);
            }
        }

        #region IEnumerable<IndexEntry> Members

        private List<IndexEntry> AllIndexEntries()
        {
            var entryCount = Count;
            var list = new List<IndexEntry>(entryCount);

            for (int i = 0; i < entryCount; i++)
            {
                list.Add(this[i]);
            }

            return list;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<IndexEntry> GetEnumerator()
        {
            return AllIndexEntries().GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Replaces entries in the staging area with entries from the specified tree.
        /// <para>
        ///   This overwrites all existing state in the staging area.
        /// </para>
        /// </summary>
        /// <param name="source">The <see cref="Tree"/> to read the entries from.</param>
        public virtual void Replace(Tree source)
        {
            using (var obj = new ObjectSafeWrapper(source.Id, repo.Handle))
            {
                Proxy.git_index_read_fromtree(this, obj.ObjectPtr);
            }

            UpdatePhysicalIndex();
        }

        /// <summary>
        /// Clears all entries the index. This is semantically equivalent to
        /// creating an empty tree object and resetting the index to that tree.
        /// <para>
        ///   This overwrites all existing state in the staging area.
        /// </para>
        /// </summary>
        public virtual void Clear()
        {
            Proxy.git_index_clear(this);
            UpdatePhysicalIndex();
        }

        private string RemoveFromIndex(string relativePath)
        {
            Proxy.git_index_remove_bypath(handle, relativePath);

            return relativePath;
        }

        private void UpdatePhysicalIndex()
        {
            Proxy.git_index_write(handle);
        }

        internal void Replace(TreeChanges changes)
        {
            foreach (TreeEntryChanges treeEntryChanges in changes)
            {
                switch (treeEntryChanges.Status)
                {
                    case ChangeKind.Unmodified:
                        continue;

                    case ChangeKind.Added:
                        RemoveFromIndex(treeEntryChanges.Path);
                        continue;

                    case ChangeKind.Deleted:
                        /* Fall through */
                    case ChangeKind.Modified:
                        ReplaceIndexEntryWith(treeEntryChanges);
                        continue;

                    default:
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Entry '{0}' bears an unexpected ChangeKind '{1}'", treeEntryChanges.Path, treeEntryChanges.Status));
                }
            }

            UpdatePhysicalIndex();
        }

        /// <summary>
        ///  Gets the conflicts that exist.
        /// </summary>
        public virtual ConflictCollection Conflicts
        {
            get
            {
                return conflicts;
            }
        }

        private void ReplaceIndexEntryWith(TreeEntryChanges treeEntryChanges)
        {
            var indexEntry = new GitIndexEntry
            {
                Mode = (uint)treeEntryChanges.OldMode,
                Id = treeEntryChanges.OldOid.Oid,
                Path = StrictFilePathMarshaler.FromManaged(treeEntryChanges.OldPath),
            };

            Proxy.git_index_add(handle, indexEntry);
            EncodingMarshaler.Cleanup(indexEntry.Path);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "Count = {0}", Count);
            }
        }
    }
}
