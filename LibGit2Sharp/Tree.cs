using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A container which references a list of other <see cref="Tree"/>s and <see cref="Blob"/>s.
    /// </summary>
    public class Tree : GitObject, IEnumerable<TreeEntry>
    {
        private readonly Repository repo;
        private readonly FilePath path;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Tree()
        { }

        internal Tree(ObjectId id, FilePath path, int treeEntriesCount, Repository repository)
            : base(id)
        {
            Count = treeEntriesCount;
            repo = repository;
            this.path = path ?? "";
        }

        /// <summary>
        ///   Gets the number of <see cref = "TreeEntry" /> immediately under this <see cref = "Tree" />.
        /// </summary>
        public virtual int Count { get; private set; }

        /// <summary>
        ///   Gets the <see cref = "TreeEntry" /> pointed at by the <paramref name = "relativePath" /> in this <see cref = "Tree" /> instance.
        /// </summary>
        /// <param name = "relativePath">The relative path to the <see cref = "TreeEntry" /> from this instance.</param>
        /// <returns><c>null</c> if nothing has been found, the <see cref = "TreeEntry" /> otherwise.</returns>
        public virtual TreeEntry this[string relativePath]
        {
            get { return RetrieveFromPath(relativePath); }
        }

        private TreeEntry RetrieveFromPath(FilePath relativePath)
        {
            if (relativePath.IsNullOrEmpty())
            {
                return null;
            }

            using (var obj = new ObjectSafeWrapper(Id, repo))
            {
                GitObjectSafeHandle objectPtr;

                int res = NativeMethods.git_tree_get_subtree(out objectPtr, obj.ObjectPtr, relativePath);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.Success(res);

                string posixPath = relativePath.Posix;
                string filename = posixPath.Split('/').Last();

                TreeEntrySafeHandle handle = NativeMethods.git_tree_entry_byname(objectPtr, filename);
                objectPtr.SafeDispose();

                if (handle.IsInvalid)
                {
                    return null;
                }

                string parentPath = posixPath.Substring(0, posixPath.Length - filename.Length);
                return new TreeEntry(handle, Id, repo, path.Combine(parentPath));
            }
        }

        /// <summary>
        ///   Gets the <see cref = "Tree" />s immediately under this <see cref = "Tree" />.
        /// </summary>
        public virtual IEnumerable<Tree> Trees
        {
            get
            {
                return this
                    .Where(e => e.Type == GitObjectType.Tree)
                    .Select(e => e.Target)
                    .Cast<Tree>();
            }
        }

        /// <summary>
        ///   Gets the <see cref = "Blob" />s immediately under this <see cref = "Tree" />.
        /// </summary>
        public virtual IEnumerable<Blob> Blobs
        {
            get
            {
                return this
                    .Where(e => e.Type == GitObjectType.Blob)
                    .Select(e => e.Target)
                    .Cast<Blob>();
            }
        }

        internal string Path
        {
            get { return path.Native; }
        }

        #region IEnumerable<TreeEntry> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<TreeEntry> GetEnumerator()
        {
            using (var obj = new ObjectSafeWrapper(Id, repo))
            {
                for (uint i = 0; i < Count; i++)
                {
                    TreeEntrySafeHandle handle = NativeMethods.git_tree_entry_byindex(obj.ObjectPtr, i);
                    yield return new TreeEntry(handle, Id, repo, path);
                }
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        internal static Tree BuildFromPtr(GitObjectSafeHandle obj, ObjectId id, Repository repo, FilePath path)
        {
            var tree = new Tree(id, path, (int)NativeMethods.git_tree_entrycount(obj), repo);
            return tree;
        }
    }
}
