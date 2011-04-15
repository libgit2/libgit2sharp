using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Tree : GitObject
    {
        private IntPtr _tree;
        private Repository _repo;

        internal Tree(ObjectId id)
            : base(id)
        {
        }

        public int GetCount ()
        { 
                return NativeMethods.git_tree_entrycount(_tree);
        }

        internal static Tree BuildFromPtr(IntPtr obj, ObjectId id, Repository repo)
        {
            var tree = new Tree(id);
            tree._tree = obj;
            tree._repo = repo;
            return tree;
        }

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
    }
}