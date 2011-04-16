using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class TreeEntry
    {
        private IntPtr _entry;
        private readonly Repository _repo;

        public TreeEntry(IntPtr obj, Repository repo)
        {
            _entry = obj;
            _repo = repo;
            Attributes = NativeMethods.git_tree_entry_attributes(_entry);
            Name = NativeMethods.git_tree_entry_name(_entry);
            Target = new ObjectId((GitOid) Marshal.PtrToStructure(NativeMethods.git_tree_entry_id(obj), typeof (GitOid)));
        }

        public ObjectId Target { get; private set; }

        public Blob Blob { get { return _repo.Lookup<Blob>(Target); } }
        public Tree Tree { get { return _repo.Lookup<Tree>(Target); } }


        public int Attributes { get; private set; }
        public string Name { get; private set; }

        public GitObject Object
        {
            get { return _repo.Lookup(Target); }
        }
    }
}