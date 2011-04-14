using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class TreeEntry
    {
        private IntPtr _entry;

        public TreeEntry(IntPtr intPtr)
        {
            _entry = intPtr;
        }

        public string Name { get { return NativeMethods.git_tree_entry_name(_entry); } }
        public string Sha
        {
            get
            {
                IntPtr gitTreeEntryId = NativeMethods.git_tree_entry_id(_entry);

                var id = new ObjectId((GitOid) Marshal.PtrToStructure(gitTreeEntryId, typeof (GitOid)));

                return id.Sha;
            }
        }
    }
}