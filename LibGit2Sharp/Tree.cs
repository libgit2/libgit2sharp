using System;
using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Tree : GitObject, IEnumerable<TreeEntry>
    {
        private IntPtr _tree;
        private Repository _repo;

        internal Tree(ObjectId id)
            : base(id)
        {
        }

        internal static Tree BuildFromPtr(IntPtr obj, ObjectId id, Repository repo)
        {
            var tree = new Tree(id);
            tree._tree = obj;
            tree._repo = repo;
            return tree;
        }

        public int Count { get { return NativeMethods.git_tree_entrycount(_tree); } }

        public TreeEntry this[int i]
        {
            get
            {
                var obj = NativeMethods.git_tree_entry_byindex(_tree, i);
                return new TreeEntry(obj, _repo);
            }
        }

        public TreeEntry this[string name]
        {
            get
            {
                var obj = NativeMethods.git_tree_entry_byname(_tree, name);
                return new TreeEntry(obj, _repo);
            }
        }

        public IEnumerator<TreeEntry> GetEnumerator()
        {
            int max = Count;
            for (int i = 0; i < max; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}