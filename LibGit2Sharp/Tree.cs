using System;
using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Tree : GitObject, IEnumerable<TreeEntry>, IDisposable
    {
        private Repository _repo;

        internal Tree(ObjectId id, IntPtr obj)
            : base(obj, id)
        {
        }

        internal static Tree BuildFromPtr(IntPtr obj, ObjectId id, Repository repo)
        {
            var tree = new Tree(id, obj);
            tree._repo = repo;
            return tree;
        }

        public int Count { get { return NativeMethods.git_tree_entrycount(Obj); } }

        public TreeEntry this[int i]
        {
            get
            {
                var e = NativeMethods.git_tree_entry_byindex(Obj, i);
                var treeEntry = new TreeEntry(e, _repo);
                return treeEntry;
            }
        }

        public TreeEntry this[string name]
        {
            get
            {
                var e = NativeMethods.git_tree_entry_byname(Obj, name);
                var treeEntry = new TreeEntry(e, _repo);
                return treeEntry;
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