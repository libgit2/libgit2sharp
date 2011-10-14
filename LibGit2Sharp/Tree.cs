using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Tree : GitObject, IEnumerable<TreeEntry>
    {
        private Repository repo;

        internal Tree(ObjectId id)
            : base(id)
        {
        }

        /// <summary>
        ///   Gets the number of <see cref = "TreeEntry" /> immediately under this <see cref = "Tree" />.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        ///   Gets the <see cref = "TreeEntry" /> pointed at by the <paramref name = "relativePath" /> in this <see cref = "Tree" /> instance.
        /// </summary>
        /// <param name = "relativePath">The relative path to the <see cref = "TreeEntry" /> from this instance.</param>
        /// <returns><c>null</c> if nothing has been found, the <see cref = "TreeEntry" /> otherwise.</returns>
        public TreeEntry this[string relativePath]
        {
            get { return RetrieveFromPath(PosixPathHelper.ToPosix(relativePath)); }
        }

        private TreeEntry RetrieveFromPath(string relativePath)
        {
            string[] pathSegments = relativePath.Split('/');

            Tree tree = this;

            for (int i = 0; i < pathSegments.Length - 1; i++)
            {
                TreeEntry entry = tree.RetrieveFromName(pathSegments[i]);

                if (entry == null || entry.Type != GitObjectType.Tree)
                {
                    return null;
                }

                tree = (Tree)entry.Target;
            }

            return tree.RetrieveFromName(pathSegments[pathSegments.Length - 1]);
        }

        private TreeEntry RetrieveFromName(string entryName)
        {
            using (var obj = new ObjectSafeWrapper(Id, repo))
            {
                IntPtr e = NativeMethods.git_tree_entry_byname(obj.ObjectPtr, entryName);

                if (e == IntPtr.Zero)
                {
                    return null;
                }

                return new TreeEntry(e, Id, repo);
            }
        }

        /// <summary>
        ///   Gets the <see cref = "Tree" />s immediately under this <see cref = "Tree" />.
        /// </summary>
        public IEnumerable<Tree> Trees
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
        public IEnumerable<Blob> Files
        {
            get
            {
                return this
                    .Where(e => e.Type == GitObjectType.Blob)
                    .Select(e => e.Target)
                    .Cast<Blob>();
            }
        }

        #region IEnumerable<TreeEntry> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator<TreeEntry> GetEnumerator()
        {
            using (var obj = new ObjectSafeWrapper(Id, repo))
            {
                for (uint i = 0; i < Count; i++)
                {
                    IntPtr e = NativeMethods.git_tree_entry_byindex(obj.ObjectPtr, i);
                    yield return new TreeEntry(e, Id, repo);
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

        internal static Tree BuildFromPtr(IntPtr obj, ObjectId id, Repository repo)
        {
            var tree = new Tree(id) { repo = repo, Count = (int)NativeMethods.git_tree_entrycount(obj) };
            return tree;
        }
    }
}
