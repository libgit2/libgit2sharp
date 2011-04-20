using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class TreeEntry
    {
        private readonly Repository repo;
        private GitObject target;
        private readonly ObjectId targetOid;

        public TreeEntry(IntPtr obj, Repository repo)
        {
            this.repo = repo;
            IntPtr gitTreeEntryId = NativeMethods.git_tree_entry_id(obj);
            targetOid = new ObjectId((GitOid)Marshal.PtrToStructure(gitTreeEntryId, typeof(GitOid)));

            Attributes = NativeMethods.git_tree_entry_attributes(obj);
            Name = NativeMethods.git_tree_entry_name(obj);
        }

        public int Attributes { get; private set; }
        
        public string Name { get; private set; }

        public GitObject Target { get { return target ?? (target = RetreiveTreeEntryTarget()); } }

        private GitObject RetreiveTreeEntryTarget()
        {
            GitObject treeEntryTarget = repo.Lookup(targetOid);
            Ensure.ArgumentConformsTo(treeEntryTarget.GetType(), t => typeof(Blob).IsAssignableFrom(t) || typeof(Tree).IsAssignableFrom(t), "treeEntryTarget");
            return treeEntryTarget;
        }
    }
}