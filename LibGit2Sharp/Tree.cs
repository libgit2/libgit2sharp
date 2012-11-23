using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A container which references a list of other <see cref="Tree"/>s and <see cref="Blob"/>s.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Tree : GitObject, IEnumerable<TreeEntry>
    {
        private readonly FilePath path;

        private readonly ILazy<int> lazyCount;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Tree()
        { }

        internal Tree(Repository repo, ObjectId id, FilePath path)
            : base(repo, id)
        {
            this.path = path ?? "";

            lazyCount = GitObjectLazyGroup.Singleton(repo, id, Proxy.git_tree_entrycount);
        }

        /// <summary>
        ///   Gets the number of <see cref = "TreeEntry" /> immediately under this <see cref = "Tree" />.
        /// </summary>
        public virtual int Count { get { return lazyCount.Value; } }

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

            using (TreeEntrySafeHandle_Owned treeEntryPtr = Proxy.git_tree_entry_bypath(repo.Handle, Id, relativePath))
            {
                if (treeEntryPtr == null)
                {
                    return null;
                }

                string posixPath = relativePath.Posix;
                string filename = posixPath.Split('/').Last();
                string parentPath = posixPath.Substring(0, posixPath.Length - filename.Length);
                return new TreeEntry(treeEntryPtr, Id, repo, path.Combine(parentPath));
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
            using (var obj = new ObjectSafeWrapper(Id, repo.Handle))
            {
                for (uint i = 0; i < Count; i++)
                {
                    TreeEntrySafeHandle handle = Proxy.git_tree_entry_byindex(obj.ObjectPtr, i);
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

        private string DebuggerDisplay
        {
            get { return string.Format("{0}, Count = {1}", Id.ToString(7), Count); }
        }
    }
}
