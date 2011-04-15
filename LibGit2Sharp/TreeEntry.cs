using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class TreeEntry : GitObject
    {
        private IntPtr _entry;
        private readonly Repository _repo;

        public TreeEntry(IntPtr intPtr, Repository repo) : base(null)
        {
            _entry = intPtr;
            _repo = repo;
            IntPtr gitTreeEntryId = NativeMethods.git_tree_entry_id(_entry);

            Id = new ObjectId((GitOid)Marshal.PtrToStructure(gitTreeEntryId, typeof(GitOid)));
            Attributes = NativeMethods.git_tree_entry_attributes(_entry);
            Name = NativeMethods.git_tree_entry_name(_entry);
        }

        public int Attributes { get; private set; }
        public string Name { get; private set; }

        public GitObject Object
        {
            get
            {
                IntPtr obj;
                var res = NativeMethods.git_tree_entry_2object(out obj, _repo.Handle, _entry);
                Ensure.Success(res);
                var resObj = CreateFromPtr(obj, RetrieveObjectIfOf(obj), _repo);
                return resObj;
            }
        }
    }
}