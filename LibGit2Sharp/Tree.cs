using System;
using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Tree : GitObject, IEnumerable<TreeEntry>
    {
        private Repository _repo;

        internal Tree(ObjectId id)
            : base(id)
        {
        }

        public int Count { get; private set; }

        public TreeEntry this[int i]
        {
            get
            {
                using (var obj = new ObjectSafeWrapper(Id, _repo))
                                                      {
                                                          IntPtr e = NativeMethods.git_tree_entry_byindex(obj.ObjectPtr, i);
                                                          return new TreeEntry(e, _repo);
                                                      }
            }
        }

        public TreeEntry this[string name]
        {
            get
            {
                using (var obj = new ObjectSafeWrapper(Id, _repo))
                                                      {
                                                          IntPtr e = NativeMethods.git_tree_entry_byname(obj.ObjectPtr, name);
                                                          return new TreeEntry(e, _repo);
                                                      }
            }
        }

        #region IEnumerable<TreeEntry> Members

        public IEnumerator<TreeEntry> GetEnumerator()
        {
            using(var obj = new ObjectSafeWrapper(Id, _repo))
            {
                for (int i = 0; i < Count; i++)
                {
                    IntPtr e = NativeMethods.git_tree_entry_byindex(obj.ObjectPtr, i);
                    yield return new TreeEntry(e, _repo);
                }    
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        internal static Tree BuildFromPtr(IntPtr obj, ObjectId id, Repository repo)
        {
            var tree = new Tree(id) {_repo = repo, Count = NativeMethods.git_tree_entrycount(obj)};
            return tree;
        }
    }
}