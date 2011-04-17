using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class TreeEntry
    {
        private readonly Repository _repo;

        public TreeEntry(IntPtr obj, Repository repo)
        {
            _repo = repo;
            IntPtr gitTreeEntryId = NativeMethods.git_tree_entry_id(obj);

            Target = new ObjectId((GitOid)Marshal.PtrToStructure(gitTreeEntryId, typeof(GitOid)));
            Attributes = NativeMethods.git_tree_entry_attributes(obj);
            Name = NativeMethods.git_tree_entry_name(obj);
        }

        public ObjectId Target { get; private set; }
        
        public Blob Blob { get { return _repo.Lookup<Blob>(Target); } }
        
        public Tree Tree { get { return _repo.Lookup<Tree>(Target); } }

        public int Attributes { get; private set; }
        
        public string Name { get; private set; }

        public GitObject Object { get { return _repo.Lookup(Target); } }
    }
}