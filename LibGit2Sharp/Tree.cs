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

        public int Count { get; private set; }

        public TreeEntry this[string name]
        {
            get
            {
                using (var obj = new ObjectSafeWrapper(Id, repo))
                {
                    IntPtr e = NativeMethods.git_tree_entry_byname(obj.ObjectPtr, name);
                    return new TreeEntry(e, Id, repo);
                }
            }
        }

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
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
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
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
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