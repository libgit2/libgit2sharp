using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
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
        private readonly IndexHandle handle;
        private readonly Repository repo;
        private readonly ConflictCollection conflicts;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Index()
        { }

        internal Index(IndexHandle handle, Repository repo)
        {
            this.repo = repo;
            this.handle = handle;
            conflicts = new ConflictCollection(this);
        }

        internal Index(Repository repo)
            : this(Proxy.git_repository_index(repo.Handle), repo)
        {
            repo.RegisterForCleanup(handle);
        }

        internal Index(Repository repo, string indexPath)
        {
            this.repo = repo;

            handle = Proxy.git_index_open(indexPath);
            Proxy.git_repository_set_index(repo.Handle, handle);
            conflicts = new ConflictCollection(this);

            repo.RegisterForCleanup(handle);
        }

        internal IndexHandle Handle
        {
            get { return handle; }
        }

        /// <summary>
        /// Gets the number of <see cref="IndexEntry"/> in the <see cref="Index"/>.
        /// </summary>
        public virtual int Count
        {
            get { return Proxy.git_index_entrycount(handle); }
        }

        /// <summary>
        /// Determines if the <see cref="Index"/> is free from conflicts.
        /// </summary>
        public virtual bool IsFullyMerged
        {
            get { return !Proxy.git_index_has_conflicts(handle); }
        }

        /// <summary>
        /// Gets the <see cref="IndexEntry"/> with the specified relative path.
        /// </summary>
        public virtual unsafe IndexEntry this[string path]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(path, "path");

                git_index_entry* entry = Proxy.git_index_get_bypath(handle, path, 0);
                return IndexEntry.BuildFromPtr(entry);
            }
        }

        private unsafe IndexEntry this[int index]
        {
            get
            {
                git_index_entry* entryHandle = Proxy.git_index_get_byindex(handle, (UIntPtr)index);
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
        /// Replaces entries in the <see cref="Index"/> with entries from the specified <see cref="Tree"/>.
        /// <para>
        ///   This overwrites all existing state in the <see cref="Index"/>.
        /// </para>
        /// </summary>
        /// <param name="source">The <see cref="Tree"/> to read the entries from.</param>
        public virtual void Replace(Tree source)
        {
            using (var obj = new ObjectSafeWrapper(source.Id, repo.Handle))
            {
                Proxy.git_index_read_fromtree(this, obj.ObjectPtr);
            }
        }

        /// <summary>
        /// Clears all entries the <see cref="Index"/>. This is semantically equivalent to
        /// creating an empty <see cref="Tree"/> object and resetting the <see cref="Index"/> to that <see cref="Tree"/>.
        /// <para>
        ///   This overwrites all existing state in the <see cref="Index"/>.
        /// </para>
        /// </summary>
        public virtual void Clear()
        {
            Proxy.git_index_clear(this);
        }

        private void RemoveFromIndex(string relativePath)
        {
            Proxy.git_index_remove_bypath(handle, relativePath);
        }

        /// <summary>
        /// Removes a specified entry from the <see cref="Index"/>.
        /// </summary>
        /// <param name="indexEntryPath">The path of the <see cref="Index"/> entry to be removed.</param>
        public virtual void Remove(string indexEntryPath)
        {
            Ensure.ArgumentNotNull(indexEntryPath, "indexEntryPath");
            RemoveFromIndex(indexEntryPath);
        }

        /// <summary>
        /// Adds a file from the working directory in the <see cref="Index"/>.
        /// <para>
        ///   If an entry with the same path already exists in the <see cref="Index"/>,
        ///   the newly added one will overwrite it.
        /// </para>
        /// </summary>
        /// <param name="pathInTheWorkdir">The path, in the working directory, of the file to be added.</param>
        public virtual void Add(string pathInTheWorkdir)
        {
            Ensure.ArgumentNotNull(pathInTheWorkdir, "pathInTheWorkdir");
            Proxy.git_index_add_bypath(handle, pathInTheWorkdir);
        }

        /// <summary>
        /// Adds an entry in the <see cref="Index"/> from a <see cref="Blob"/>.
        /// <para>
        ///   If an entry with the same path already exists in the <see cref="Index"/>,
        ///   the newly added one will overwrite it.
        /// </para>
        /// </summary>
        /// <param name="blob">The <see cref="Blob"/> which content should be added to the <see cref="Index"/>.</param>
        /// <param name="indexEntryPath">The path to be used in the <see cref="Index"/>.</param>
        /// <param name="indexEntryMode">Either <see cref="Mode.NonExecutableFile"/>, <see cref="Mode.ExecutableFile"/>
        /// or <see cref="Mode.SymbolicLink"/>.</param>
        public virtual void Add(Blob blob, string indexEntryPath, Mode indexEntryMode)
        {
            Ensure.ArgumentConformsTo(indexEntryMode, m => m.HasAny(TreeEntryDefinition.BlobModes), "indexEntryMode");
            Ensure.ArgumentNotNull(blob, "blob");
            Ensure.ArgumentNotNull(indexEntryPath, "indexEntryPath");
            AddEntryToTheIndex(indexEntryPath, blob.Id, indexEntryMode);
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
                    case ChangeKind.Modified:
                        AddEntryToTheIndex(treeEntryChanges.OldPath,
                                           treeEntryChanges.OldOid,
                                           treeEntryChanges.OldMode);
                        continue;

                    default:
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                          "Entry '{0}' bears an unexpected ChangeKind '{1}'",
                                                                          treeEntryChanges.Path,
                                                                          treeEntryChanges.Status));
                }
            }
        }

        /// <summary>
        ///  Gets the conflicts that exist.
        /// </summary>
        public virtual ConflictCollection Conflicts
        {
            get { return conflicts; }
        }

        private unsafe void AddEntryToTheIndex(string path, ObjectId id, Mode mode)
        {
            IntPtr pathPtr = StrictFilePathMarshaler.FromManaged(path);
            var indexEntry = new git_index_entry
            {
                mode = (uint)mode,
                path = (char*)pathPtr,
            };
            Marshal.Copy(id.RawId, 0, new IntPtr(indexEntry.id.Id), GitOid.Size);

            Proxy.git_index_add(handle, &indexEntry);
            EncodingMarshaler.Cleanup(pathPtr);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "Count = {0}", Count);
            }
        }

        /// <summary>
        /// Replaces entries in the <see cref="Index"/> with entries from the specified <see cref="Commit"/>.
        /// </summary>
        /// <param name="commit">The target <see cref="Commit"/> object.</param>
        public virtual void Replace(Commit commit)
        {
            Replace(commit, null, null);
        }

        /// <summary>
        /// Replaces entries in the <see cref="Index"/> with entries from the specified <see cref="Commit"/>.
        /// </summary>
        /// <param name="commit">The target <see cref="Commit"/> object.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be considered.</param>
        public virtual void Replace(Commit commit, IEnumerable<string> paths)
        {
            Replace(commit, paths, null);
        }

        /// <summary>
        /// Replaces entries in the <see cref="Index"/> with entries from the specified <see cref="Commit"/>.
        /// </summary>
        /// <param name="commit">The target <see cref="Commit"/> object.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be considered.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public virtual void Replace(Commit commit, IEnumerable<string> paths, ExplicitPathsOptions explicitPathsOptions)
        {
            Ensure.ArgumentNotNull(commit, "commit");

            using (var changes = repo.Diff.Compare<TreeChanges>(commit.Tree, DiffTargets.Index, paths, explicitPathsOptions, new CompareOptions { Similarity = SimilarityOptions.None }))
            {
                Replace(changes);
            }
        }

        /// <summary>
        /// Write the contents of this <see cref="Index"/> to disk
        /// </summary>
        public virtual void Write()
        {
            Proxy.git_index_write(handle);
        }

        /// <summary>
        /// Write the contents of this <see cref="Index"/> to a tree
        /// </summary>
        /// <returns></returns>
        public virtual Tree WriteToTree()
        {
            var treeId = Proxy.git_index_write_tree_to(this.handle, this.repo.Handle);
            var result = this.repo.Lookup<Tree>(treeId);
            return result;
        }
    }
}
