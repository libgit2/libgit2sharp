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
        /// Promotes to the staging area the latest modifications of a file in the working directory (addition, updation or removal).
        ///
        /// If this path is ignored by configuration then it will not be staged unless <see cref="StageOptions.IncludeIgnored"/> is unset.
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="stageOptions">If set, determines how paths will be staged.</param>
        [Obsolete("This method will be removed in the next release. Use Repository.Stage instead.")]
        public virtual void Stage(string path, StageOptions stageOptions = null)
        {
            repo.Stage(path, stageOptions);
        }

        /// <summary>
        /// Promotes to the staging area the latest modifications of a collection of files in the working directory (addition, updation or removal).
        ///
        /// Any paths (even those listed explicitly) that are ignored by configuration will not be staged unless <see cref="StageOptions.IncludeIgnored"/> is unset.
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="stageOptions">If set, determines how paths will be staged.</param>
        [Obsolete("This method will be removed in the next release. Use Repository.Stage instead.")]
        public virtual void Stage(IEnumerable<string> paths, StageOptions stageOptions = null)
        {
            repo.Stage(paths, stageOptions);
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="path"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        [Obsolete("This method will be removed in the next release. Use Repository.Unstage instead.")]
        public virtual void Unstage(string path, ExplicitPathsOptions explicitPathsOptions = null)
        {
            repo.Unstage(path, explicitPathsOptions);
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a collection of file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        [Obsolete("This method will be removed in the next release. Use Repository.Unstage instead.")]
        public virtual void Unstage(IEnumerable<string> paths, ExplicitPathsOptions explicitPathsOptions = null)
        {
            repo.Unstage(paths, explicitPathsOptions);
        }

        /// <summary>
        /// Moves and/or renames a file in the working directory and promotes the change to the staging area.
        /// </summary>
        /// <param name="sourcePath">The path of the file within the working directory which has to be moved/renamed.</param>
        /// <param name="destinationPath">The target path of the file within the working directory.</param>
        [Obsolete("This method will be removed in the next release. Use Repository.Move instead.")]
        public virtual void Move(string sourcePath, string destinationPath)
        {
            repo.Move(new[] { sourcePath }, new[] { destinationPath });
        }

        /// <summary>
        /// Moves and/or renames a collection of files in the working directory and promotes the changes to the staging area.
        /// </summary>
        /// <param name="sourcePaths">The paths of the files within the working directory which have to be moved/renamed.</param>
        /// <param name="destinationPaths">The target paths of the files within the working directory.</param>
        [Obsolete("This method will be removed in the next release. Use Repository.Move instead.")]
        public virtual void Move(IEnumerable<string> sourcePaths, IEnumerable<string> destinationPaths)
        {
            repo.Move(sourcePaths, destinationPaths);
        }

        /// <summary>
        /// Removes a file from the staging area, and optionally removes it from the working directory as well.
        /// <para>
        ///   If the file has already been deleted from the working directory, this method will only deal
        ///   with promoting the removal to the staging area.
        /// </para>
        /// <para>
        ///   The default behavior is to remove the file from the working directory as well.
        /// </para>
        /// <para>
        ///   When not passing a <paramref name="explicitPathsOptions"/>, the passed path will be treated as
        ///   a pathspec. You can for example use it to pass the relative path to a folder inside the working directory,
        ///   so that all files beneath this folders, and the folder itself, will be removed.
        /// </para>
        /// </summary>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the file from the working directory, False otherwise.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="path"/> will be treated as an explicit path.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        [Obsolete("This method will be removed in the next release. Use Repository.Remove instead.")]
        public virtual void Remove(string path, bool removeFromWorkingDirectory = true, ExplicitPathsOptions explicitPathsOptions = null)
        {
           repo.Remove(new[] { path }, removeFromWorkingDirectory, explicitPathsOptions);
        }

        /// <summary>
        /// Removes a collection of fileS from the staging, and optionally removes them from the working directory as well.
        /// <para>
        ///   If a file has already been deleted from the working directory, this method will only deal
        ///   with promoting the removal to the staging area.
        /// </para>
        /// <para>
        ///   The default behavior is to remove the files from the working directory as well.
        /// </para>
        /// <para>
        ///   When not passing a <paramref name="explicitPathsOptions"/>, the passed paths will be treated as
        ///   a pathspec. You can for example use it to pass the relative paths to folders inside the working directory,
        ///   so that all files beneath these folders, and the folders themselves, will be removed.
        /// </para>
        /// </summary>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the files from the working directory, False otherwise.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        [Obsolete("This method will be removed in the next release. Use Repository.Remove instead.")]
        public virtual void Remove(IEnumerable<string> paths, bool removeFromWorkingDirectory = true, ExplicitPathsOptions explicitPathsOptions = null)
        {
            repo.Remove(paths, removeFromWorkingDirectory, explicitPathsOptions);
        }
        
        /// <summary>
        /// Replaces entries in the staging area with entries from the specified tree.
        /// <para>
        ///   This overwrites all existing state in the staging area.
        /// </para>
        /// </summary>
        /// <param name="source">The <see cref="Tree"/> to read the entries from.</param>
        [Obsolete("This method will be removed in the next release. Use Replace instead.")]
        public virtual void Reset(Tree source)
        {
            Replace(source);
        }

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

        /// <summary>
        /// Retrieves the state of a file in the working directory, comparing it against the staging area and the latest commmit.
        /// </summary>
        /// <param name="filePath">The relative path within the working directory to the file.</param>
        /// <returns>A <see cref="FileStatus"/> representing the state of the <paramref name="filePath"/> parameter.</returns>
        [Obsolete("This method will be removed in the next release. Use Repository.RetrieveStatus instead.")]
        public virtual FileStatus RetrieveStatus(string filePath)
        {
            return repo.RetrieveStatus(filePath);
        }

        /// <summary>
        /// Retrieves the state of all files in the working directory, comparing them against the staging area and the latest commmit.
        /// </summary>
        /// <param name="options">If set, the options that control the status investigation.</param>
        /// <returns>A <see cref="RepositoryStatus"/> holding the state of all the files.</returns>
        [Obsolete("This method will be removed in the next release. Use Repository.RetrieveStatus instead.")]
        public virtual RepositoryStatus RetrieveStatus(StatusOptions options = null)
        {
            return repo.RetrieveStatus(options);
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
